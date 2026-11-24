# 🍷 Vinheria Agnello — Arquitetura de Microsserviços & APIs
> Sistema distribuído baseado em microsserviços, API Gateway e mensageria assíncrona aplicado ao ecossistema vinícola sob a filosofia **"Da Uva à Taça" (Direct-to-Consumer - D2C)**.

---

## 📌 1. Visão Geral do Projeto

A **Vinheria Agnello** opera sob um modelo de negócio direto com o consumidor (sem intermediários), valorizando transparência de ponta a ponta e excelência na conservação. 

Para sustentar o crescimento operacional, o sistema evoluiu de um modelo monolítico/acoplado para uma **arquitetura corporativa orientada a microsserviços e eventos**, integrando desde o chão de fábrica e sensores IoT de adega até o checkout omnicanal e a expedição de pedidos.

---

## 🏛️ 2. Diagrama de Arquitetura

┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ Produção │ │ Qualidade │ │Rastreamento │ │Vendas/Pedidos│ │Estoque/Logís.│
│ (Porta 5001) │ │ (Porta 5002) │ │(Porta 5003) │ │(Porta 5004) │ │(Porta 5005) │
└──────┬───────┘ └──────┬───────┘ └──────┬───────┘ └──────┬───────┘ └──────┬───────┘
│ │ │ │ │
(producao.db) (qualidade.db) (rastreabilidade.db) (pedidos.db) (estoque.db)
│ │ ▲ │ ▲
│ │ │ │ │
└───────────────────┴───────────┬───────┴───────────────────┴───────────────────┘
▼
┌──────────────────────┐
│ MQTT Event Broker │ ◄─── Telemetria IoT
│ (Eclipse Mosquitto) │ (ESP32: DHT22 e LDR)
└──────────────────────┘

---

## 🧩 3. Identificação dos 5 Microsserviços

| # | Microsserviço | Responsabilidade de Domínio | Banco de Dados | Comunicação |
| :-: | :--- | :--- | :--- | :--- |
| **1** | **Produção** | Registro de safras, uvas, tanques de fermentação, barricas de carvalho e criação de lotes. | `producao.db` (SQLite) | REST (Entrada) / MQTT (Publica) |
| **2** | **Qualidade & Monitoramento** | Ingestão de telemetria dos sensores da adega (ESP32 via MQTT) e emissão de laudos técnicos enológicos. | `qualidade.db` (SQLite) | MQTT (Ingestão) / REST (Laudo) |
| **3** | **Rastreabilidade** | Vitrine de transparência ("Da Uva à Taça"). Consolida assincronamente os dados para consulta pública via QR Code. | `rastreabilidade.db` (SQLite) | MQTT (Consumo) / REST (Consulta) |
| **4** | **Vendas e Pedidos** | Gestão do catálogo, checkout D2C, snapshot dos dados cadastrais do cliente e ciclo de vida do pedido. | `pedidos.db` (SQLite) | REST (Entrada) / MQTT (Saga) |
| **5** | **Estoque & Logística** | Controle físico de saldo de garrafas, regras de frete e separação/reserva de estoque para expedição. | `estoque.db` (SQLite) | REST (CRUD) / MQTT (Saga) |

---

## 📐 4. Padrões de Arquitetura Aplicados

* **API Gateway Pattern (Ocelot):** Centraliza o ponto de entrada externo na porta `8080`. Encapsula os 5 microsserviços para que clientes externos não precisem conhecer a topologia interna nem portas individuais.
* **Database per Service Pattern:** Cada serviço possui seu próprio arquivo SQLite isolado (`producao.db`, `qualidade.db`, etc.) montado em volume dedicado. Não existe compartilhamento de banco de dados entre serviços.
* **Saga Pattern por Coreografia (MQTT):** Mantém a consistência eventual de compra sem um coordenador centralizado:
  1. `Vendas` emite `vinheria/pedidos/criado` (status `Pendente`).
  2. `Estoque` consome, valida saldo físico e debita garrafas.
  3. `Estoque` emite `vinheria/estoque/resultado-reserva`.
  4. `Vendas` consome a resposta e transiciona o pedido para `Confirmado`.
* **Service Discovery (Docker Compose DNS):** O API Gateway e os serviços comunicam-se utilizando a resolução de nomes interna nativa do Docker (`http://servico-estoque:5005`, `http://servico-pedidos:5004`, etc.), eliminando servidores de discovery pesados (como Consul ou Eureka).
* **Event-Driven Architecture (EDA):** A integração da Rastreabilidade e da Qualidade é 100% desacoplada: os serviços de chão de fábrica e sensores emitem eventos no broker Mosquitto e a Rastreabilidade atualiza o modelo de leitura em segundo plano.

---

## 🛠️ 5. Tecnologias & Stacks Utilizadas (100% Open-Source)

* **Runtime:** .NET 8 SDK / C# (ASP.NET Core Web API com Controllers clássicos).
* **API Gateway:** Ocelot v23.
* **Persistência:** Entity Framework Core + SQLite.
* **Mensageria & Barramento IoT:** Eclipse Mosquitto v2 (MQTT).
* **Orquestração de Containers:** Docker & Docker Compose.

---

## 🚀 6. Como Executar o Projeto

### Pré-requisitos:
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e em execução.

### Execução em 1 comando:
Na raiz do repositório (`vinheria-agnello/`), execute:

```bash
docker compose up -d --build

