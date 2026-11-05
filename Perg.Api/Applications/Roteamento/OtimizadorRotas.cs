using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Applications.Roteamento
{
    // Entrada do otimizador. Nó 0 = origem; nós 1..n = entregas (mesma ordem da matriz).
    public class ProblemaRoteirizacao
    {
        public MatrizDistancias Matriz { get; set; } = new();

        // Por nó (índice 0 = origem, sempre 0)
        public long[] PesoGramas { get; set; } = Array.Empty<long>();
        public long[] Volumes { get; set; } = Array.Empty<long>();
        public long[] JanelaInicioS { get; set; } = Array.Empty<long>();
        public long[] JanelaFimS { get; set; } = Array.Empty<long>();

        public List<VeiculoDisponivel> Veiculos { get; set; } = new();

        // Tempo parado em cada entrega e duração máxima de cada rota
        public long TempoParadaS { get; set; }
        public long JornadaMaximaS { get; set; }

        public ObjetivoRoteirizacao Objetivo { get; set; }
        public int TempoLimiteSegundos { get; set; } = 10;

        // Espera parada acima disso entre duas entregas divide a rota em duas viagens
        public long EsperaMaximaS { get; set; } = 3600;
    }

    public class VeiculoDisponivel
    {
        public TipoVeiculo Tipo { get; set; }
        public long CapacidadeGramas { get; set; }
        public long CapacidadeVolumes { get; set; }
    }

    public class RotaOtimizada
    {
        public int VeiculoIndice { get; set; }
        public List<ParadaOtimizada> Paradas { get; set; } = new();
        public long DistanciaM { get; set; }

        // Segundos depois do horário-base em que o veículo sai da loja, e duração da saída até a volta
        public long SaidaS { get; set; }
        public long DuracaoS { get; set; }
    }

    public class ParadaOtimizada
    {
        public int No { get; set; }
        public long ChegadaS { get; set; }
    }

    public class SolucaoRoteirizacao
    {
        public List<RotaOtimizada> Rotas { get; set; } = new();
        public List<int> NosForaDaRota { get; set; } = new();
    }

    // Google OR-Tools: problema de roteamento com capacidade (peso e volumes), janelas de horário
    // e jornada máxima. Pedidos que não cabem em nenhuma rota ficam de fora (em vez de falhar tudo).
    public class OtimizadorRotas
    {
        // Deixar um pedido de fora custa muito mais do que qualquer desvio: só acontece se for impossível atender
        private const long PenalidadeForaDaRota = 100_000_000;

        public SolucaoRoteirizacao? Otimizar(ProblemaRoteirizacao problema)
        {
            int nos = problema.PesoGramas.Length;
            int quantidadeVeiculos = problema.Veiculos.Count;
            long[,] metros = problema.Matriz.Metros;
            long[,] segundos = problema.Matriz.Segundos;

            var manager = new RoutingIndexManager(nos, quantidadeVeiculos, 0);
            var routing = new RoutingModel(manager);

            // Custo de cada trecho: distância ou tempo, conforme o objetivo escolhido
            int distanciaCallback = routing.RegisterTransitCallback((long de, long para) =>
                metros[manager.IndexToNode(de), manager.IndexToNode(para)]);

            int tempoCallback = routing.RegisterTransitCallback((long de, long para) =>
            {
                int origem = manager.IndexToNode(de);
                int destino = manager.IndexToNode(para);
                long parado = origem == 0 ? 0 : problema.TempoParadaS;
                return segundos[origem, destino] + parado;
            });

            routing.SetArcCostEvaluatorOfAllVehicles(
                problema.Objetivo == ObjetivoRoteirizacao.Tempo ? tempoCallback : distanciaCallback);

            // "Menos veículos": cada veículo usado tem um custo fixo alto
            if (problema.Objetivo == ObjetivoRoteirizacao.Veiculos)
            {
                routing.SetFixedCostOfAllVehicles(1_000_000);
            }

            // Capacidade de peso e de volumes de cada veículo
            int pesoCallback = routing.RegisterUnaryTransitCallback((long indice) =>
                problema.PesoGramas[manager.IndexToNode(indice)]);
            routing.AddDimensionWithVehicleCapacity(pesoCallback, 0,
                problema.Veiculos.Select(v => v.CapacidadeGramas).ToArray(), true, "Peso");

            int volumesCallback = routing.RegisterUnaryTransitCallback((long indice) =>
                problema.Volumes[manager.IndexToNode(indice)]);
            routing.AddDimensionWithVehicleCapacity(volumesCallback, 0,
                problema.Veiculos.Select(v => v.CapacidadeVolumes).ToArray(), true, "Volumes");

            // Tempo: segundos desde a saída da loja. Espera permitida (chegar antes da janela e aguardar).
            routing.AddDimension(tempoCallback, problema.JornadaMaximaS, problema.JornadaMaximaS, true, "Tempo");
            RoutingDimension tempo = routing.GetMutableDimension("Tempo");

            for (int no = 1; no < nos; no++)
            {
                long indice = manager.NodeToIndex(no);
                tempo.CumulVar(indice).SetRange(problema.JanelaInicioS[no], problema.JanelaFimS[no]);

                // Entregar o quanto antes: cada segundo depois do início da janela (ou da saída, se não
                // tiver janela) custa 1. Assim ninguém fica horas no veículo esperando a entrega da tarde;
                // se for preciso, o otimizador usa outro veículo para quem pode receber já.
                tempo.SetCumulVarSoftUpperBound(indice, problema.JanelaInicioS[no], 1);
                routing.AddDisjunction(new[] { indice }, PenalidadeForaDaRota);
            }

            // Os horários finais saem sempre no mais cedo possível (sem isso o OR-Tools pode devolver
            // qualquer horário viável, inclusive atrasando a saída da loja)
            for (int veiculo = 0; veiculo < quantidadeVeiculos; veiculo++)
            {
                routing.AddVariableMinimizedByFinalizer(tempo.CumulVar(routing.Start(veiculo)));
                routing.AddVariableMinimizedByFinalizer(tempo.CumulVar(routing.End(veiculo)));
            }
            for (int no = 1; no < nos; no++)
            {
                routing.AddVariableMinimizedByFinalizer(tempo.CumulVar(manager.NodeToIndex(no)));
            }

            RoutingSearchParameters parametros = operations_research_constraint_solver.DefaultRoutingSearchParameters();
            parametros.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
            parametros.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
            parametros.TimeLimit = new Duration { Seconds = problema.TempoLimiteSegundos };

            Assignment? solucao = routing.SolveWithParameters(parametros);
            if (solucao == null)
            {
                return null;
            }

            var resultado = new SolucaoRoteirizacao();

            for (int veiculo = 0; veiculo < quantidadeVeiculos; veiculo++)
            {
                var rota = new RotaOtimizada { VeiculoIndice = veiculo };
                long indice = routing.Start(veiculo);

                while (!routing.IsEnd(indice))
                {
                    int no = manager.IndexToNode(indice);
                    if (no != 0)
                    {
                        rota.Paradas.Add(new ParadaOtimizada { No = no, ChegadaS = solucao.Min(tempo.CumulVar(indice)) });
                    }

                    long proximo = solucao.Value(routing.NextVar(indice));
                    rota.DistanciaM += metros[no, manager.IndexToNode(proximo)];
                    indice = proximo;
                }

                if (rota.Paradas.Count > 0)
                {
                    // Se a primeira entrega tem janela mais tarde, o veículo sai da loja só na hora certa
                    // (em vez de sair cedo e ficar esperando na porta do cliente)
                    ParadaOtimizada primeira = rota.Paradas[0];
                    rota.SaidaS = Math.Max(0, primeira.ChegadaS - segundos[0, primeira.No]);
                    rota.DuracaoS = solucao.Min(tempo.CumulVar(indice)) - rota.SaidaS;

                    resultado.Rotas.Add(rota);
                }
            }

            for (int no = 1; no < nos; no++)
            {
                long indice = manager.NodeToIndex(no);
                if (solucao.Value(routing.NextVar(indice)) == indice)
                {
                    resultado.NosForaDaRota.Add(no);
                }
            }

            resultado.Rotas = resultado.Rotas
                .SelectMany(rota => DividirEsperasLongas(rota, problema))
                .ToList();

            return resultado;
        }

        // Se o veículo ficaria parado muito tempo entre duas entregas (ex.: esperando uma janela à tarde),
        // a rota vira duas viagens: ele volta para a loja e sai de novo na hora certa.
        // Cada viagem vira uma rota própria (R06 de manhã, R07 à tarde), que pode ir para outro entregador.
        private static List<RotaOtimizada> DividirEsperasLongas(RotaOtimizada rota, ProblemaRoteirizacao problema)
        {
            long[,] metros = problema.Matriz.Metros;
            long[,] segundos = problema.Matriz.Segundos;
            long fimAbsoluto = rota.SaidaS + rota.DuracaoS;

            var viagens = new List<RotaOtimizada>();
            var atual = new RotaOtimizada { VeiculoIndice = rota.VeiculoIndice, SaidaS = rota.SaidaS };

            for (int i = 0; i < rota.Paradas.Count; i++)
            {
                ParadaOtimizada parada = rota.Paradas[i];

                if (atual.Paradas.Count > 0)
                {
                    ParadaOtimizada anterior = atual.Paradas[^1];
                    long saiDoAnterior = anterior.ChegadaS + problema.TempoParadaS;
                    long espera = parada.ChegadaS - (saiDoAnterior + segundos[anterior.No, parada.No]);

                    long voltaNaLoja = saiDoAnterior + segundos[anterior.No, 0];
                    long novaSaida = parada.ChegadaS - segundos[0, parada.No];

                    if (espera > problema.EsperaMaximaS && voltaNaLoja <= novaSaida)
                    {
                        FecharViagem(atual, voltaNaLoja, metros);
                        viagens.Add(atual);
                        atual = new RotaOtimizada { VeiculoIndice = rota.VeiculoIndice, SaidaS = novaSaida };
                    }
                }

                atual.Paradas.Add(parada);
            }

            if (viagens.Count == 0)
            {
                return new List<RotaOtimizada> { rota };
            }

            FecharViagem(atual, fimAbsoluto, metros);
            viagens.Add(atual);
            return viagens;
        }

        private static void FecharViagem(RotaOtimizada viagem, long chegadaNaLojaS, long[,] metros)
        {
            viagem.DuracaoS = chegadaNaLojaS - viagem.SaidaS;

            int anterior = 0;
            viagem.DistanciaM = 0;
            foreach (ParadaOtimizada parada in viagem.Paradas)
            {
                viagem.DistanciaM += metros[anterior, parada.No];
                anterior = parada.No;
            }
            viagem.DistanciaM += metros[anterior, 0];
        }
    }
}
