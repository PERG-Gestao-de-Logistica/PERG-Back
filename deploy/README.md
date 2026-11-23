# Deploy de demonstração — Azure for Students

Uma VM Linux (B1s) roda a API, o Redis e o Caddy (HTTPS) com Docker. O banco fica no Azure Database for PostgreSQL.
O Kafka continua no Aiven. Tudo dentro dos serviços gratuitos de 12 meses do Azure for Students.

## 1. Ativar o Azure for Students

Em https://azure.microsoft.com/free/students, entre com o e-mail de estudante. Não pede cartão.

## 2. Banco: Azure Database for PostgreSQL

1. Portal do Azure → **Criar recurso → Azure Database for PostgreSQL → Servidor flexível**.
2. Região: a mesma que vai usar na VM. Versão 16. **Computação: Burstable, B1ms**, armazenamento 32 GB (são os limites gratuitos).
3. Autenticação: PostgreSQL. Usuário `pergadmin` e uma senha forte.
4. Rede: acesso público. Marque **Permitir acesso público de serviços do Azure** e adicione o seu IP atual.
5. Depois de criado: em **Bancos de dados**, crie o banco `perg`.
6. No pgAdmin, conecte no servidor (host `SEU-SERVIDOR.postgres.database.azure.com`, porta 5432, SSL obrigatório) e rode `banco/perg_modelo_banco.sql` no banco `perg`.

## 3. VM

1. **Criar recurso → Máquina virtual**. Imagem **Ubuntu Server 24.04**, tamanho **B1s**, mesma região do banco.
2. Autenticação por chave SSH (o portal gera e baixa a chave). Usuário `azureuser`.
3. Portas de entrada: **SSH (22), HTTP (80) e HTTPS (443)**.
4. Depois de criada: **Endereço IP público → Configuração → Rótulo do nome DNS** = `perg-api` (vira `perg-api.REGIAO.cloudapp.azure.com`).

## 4. Instalar o Docker na VM

```bash
ssh -i chave.pem azureuser@perg-api.REGIAO.cloudapp.azure.com
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker azureuser && exit
```

A B1s tem 1 GB de RAM. Para o build da imagem não faltar memória, crie 2 GB de swap (uma vez):

```bash
sudo fallocate -l 2G /swapfile && sudo chmod 600 /swapfile && sudo mkswap /swapfile && sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

## 5. Subir a API

```bash
git clone https://github.com/PERG-Gestao-de-Logistica/PERG-Back.git
cd PERG-Back/deploy
cp variaveis.exemplo .env && nano .env  # preencha tudo
mkdir -p segredos                        # se usar Kafka: copie o ca.pem do Aiven para segredos/ca.pem
docker compose up -d --build
docker compose logs -f api               # procure "Redis LIGADO" e "Kafka LIGADO"
```

Teste: `https://perg-api.REGIAO.cloudapp.azure.com/api/Saude` e `/swagger`.

## 6. Depois de no ar

- **Asaas sandbox:** troque a URL do webhook para `https://DOMINIO/api/webhooks/asaas`.
- **Google:** adicione o endereço do front em "Origens JavaScript autorizadas" do Client ID.
- **Kafka:** use o Kafka só no servidor (tire o bloco `Kafka` dos Segredos do Usuário no seu computador). Se os dois usarem os mesmos tópicos, um ambiente lê os eventos do outro.
- **Atualizar:** `git pull && docker compose up -d --build` dentro de `deploy/`.
