# ADR-001 — Monólito em camadas com dependências para dentro

- **Status:** Aceito · **Data:** 21/09/2026
- **Contexto:** Avaliação de padrão e boas práticas (RNF-03); escopo pequeno; 5 dias; um cliente de API e uma tela.
- **Alternativas:** (a) projeto único sem camadas (rápido, mas fraco em avaliação e testabilidade); (b) microsserviços/serviço separado (custo excessivo); (c) monólito modular em camadas (escolhida).
- **Decisão:** Solução única com Domain, Application, Infrastructure, Api, Reports, Desktop, Tests (SDD 2.1). Regras de negócio só em Domain/Application; DTOs da API distintos das entidades EF; DI Autofac; mapeamento manual.
- **Consequências:** (+) serviços testáveis com Moq, troca de banco isolada em Infrastructure (viabiliza o fallback do ADR-002). (-) mais projetos/boilerplate no Dia 1 (~2h em O-02). Desktop referencia Infrastructure só no composition root.
