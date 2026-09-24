#!/usr/bin/env bash
# ============================================================================
# smoke-vendas.sh — Coleção de fumaça (T-34, revisada por T-37), formato curl real.
#
# Cobre caminho feliz + 400/401/404/409 dos 3 endpoints do contrato v1.1 já
# implementados (docs/contrato-v1.1.md Seção 3.1-3.3):
#   POST /api/vendas/quitacao
#   POST /api/vendas/cancelamento
#   GET  /api/vendas/{vendaId}/status
#
# Formato: curl real (não Postman GUI). Decisão registrada em
# docs/postman/README.md — sem acesso a rede/Postman GUI/interativo neste
# ambiente sandbox, curl é o único formato que dá para efetivamente EXECUTAR
# e confirmar "100% verde contra o app local" nesta sessão (não é só
# documentação estática — cada cenário abaixo foi rodado de fato, ver
# docs/postman/evidencia-execucao-T-34.txt e evidencia-execucao-T-37.txt).
#
# Auto-contido: não depende de estado pré-semeado no banco. Os cenários 409
# são produzidos encadeando os próprios endpoints do contrato (quitar uma
# venda cria+quita; cancelar uma venda desconhecida cria+cancela — D-08),
# com vendaId único por execução (timestamp), então roda quantas vezes for
# preciso sem colidir com execuções anteriores.
#
# Uso:
#   BASE_URL=http://localhost:5034/api/vendas API_KEY=CHAVE_LOCAL_SMOKE_T34 ./smoke-vendas.sh
#   (BASE_URL default: http://localhost:5000/api/vendas — porta padrão do
#   App.config.example do Desktop; API_KEY default: valor de exemplo abaixo,
#   ajuste para o valor real de `Api:ApiKey` do seu App.config local)
#
# Nota (X-Api-Key, T-37): a partir de T-35 (Lote 8, `ApiKeyHandler`), a API
# exige `X-Api-Key` em todo endpoint exceto `GET /api/health`. Os cenários de
# sucesso/400/404/409 abaixo enviam o header (variável API_KEY); os cenários
# 8 e 9 cobrem os dois casos de `401 NAO_AUTORIZADO` do contrato v1.1 Seção 2
# (chave ausente / chave inválida) — resposta idêntica nos dois casos, como
# exige o contrato.
# ============================================================================

set -u

BASE_URL="${BASE_URL:-http://localhost:5000/api/vendas}"
API_KEY="${API_KEY:-CHAVE_LOCAL_SMOKE_T34}"
TS="$(date +%s)"
FALHAS=0
TOTAL=0

verificar() {
  local nome="$1" status_esperado="$2" status_obtido="$3" corpo="$4" grep_esperado="${5:-}"
  TOTAL=$((TOTAL + 1))
  if [ "$status_obtido" != "$status_esperado" ]; then
    echo "[FALHA] $nome — esperado HTTP $status_esperado, obtido HTTP $status_obtido"
    echo "        corpo: $corpo"
    FALHAS=$((FALHAS + 1))
    return
  fi
  if [ -n "$grep_esperado" ] && ! echo "$corpo" | grep -q "$grep_esperado"; then
    echo "[FALHA] $nome — corpo não contém '$grep_esperado'"
    echo "        corpo: $corpo"
    FALHAS=$((FALHAS + 1))
    return
  fi
  echo "[OK]    $nome — HTTP $status_obtido"
  echo "        corpo: $corpo"
}

chamar() {
  # $1 = método, $2 = path, $3 = corpo JSON (ou "" para GET). Envia sempre o
  # header X-Api-Key (T-37) — todo endpoint de vendas exige autenticação
  # desde T-35 (ApiKeyHandler).
  local metodo="$1" path="$2" corpo="${3:-}"
  if [ "$metodo" = "GET" ]; then
    curl -s -w '\n%{http_code}' -X GET "$BASE_URL$path" -H "X-Api-Key: $API_KEY"
  else
    curl -s -w '\n%{http_code}' -X "$metodo" "$BASE_URL$path" \
      -H "Content-Type: application/json" -H "X-Api-Key: $API_KEY" -d "$corpo"
  fi
}

chamar_sem_chave() {
  # Mesma assinatura de chamar(), mas SEM o header X-Api-Key — cenário 401
  # "chave ausente" (T-37).
  local metodo="$1" path="$2" corpo="${3:-}"
  if [ "$metodo" = "GET" ]; then
    curl -s -w '\n%{http_code}' -X GET "$BASE_URL$path"
  else
    curl -s -w '\n%{http_code}' -X "$metodo" "$BASE_URL$path" \
      -H "Content-Type: application/json" -d "$corpo"
  fi
}

chamar_chave_invalida() {
  # Mesma assinatura de chamar(), mas com um valor de X-Api-Key incorreto —
  # cenário 401 "chave inválida" (T-37).
  local metodo="$1" path="$2" corpo="${3:-}"
  if [ "$metodo" = "GET" ]; then
    curl -s -w '\n%{http_code}' -X GET "$BASE_URL$path" -H "X-Api-Key: chave-invalida-t37"
  else
    curl -s -w '\n%{http_code}' -X "$metodo" "$BASE_URL$path" \
      -H "Content-Type: application/json" -H "X-Api-Key: chave-invalida-t37" -d "$corpo"
  fi
}

extrair_status() { tail -n1; }
extrair_corpo() { sed '$d'; }

echo "== Coleção de fumaça T-34 — BASE_URL=$BASE_URL =="
echo ""

# --- 1. Quitação — caminho feliz (venda nova, cria + quita) ---------------
VENDA_QUIT="V-SMOKE-QUIT-$TS"
RESP=$(chamar POST /quitacao "{\"vendaId\":\"$VENDA_QUIT\",\"clienteId\":\"C-SMOKE-1\",\"valorTotal\":250.00,\"itens\":[{\"produtoId\":\"P-01\",\"quantidade\":2,\"precoUnitario\":100.00},{\"produtoId\":\"P-02\",\"quantidade\":1,\"precoUnitario\":50.00}]}")
STATUS=$(echo "$RESP" | extrair_status); CORPO=$(echo "$RESP" | extrair_corpo)
verificar "1. Quitação — caminho feliz (200 Quitada)" 200 "$STATUS" "$CORPO" '"status":"Quitada"'

# --- 2. Quitação — 400 PAYLOAD_INVALIDO (itens vazio) ----------------------
RESP=$(chamar POST /quitacao "{\"vendaId\":\"V-SMOKE-QUIT-INVALIDA-$TS\",\"clienteId\":\"C-SMOKE-1\",\"valorTotal\":0,\"itens\":[]}")
STATUS=$(echo "$RESP" | extrair_status); CORPO=$(echo "$RESP" | extrair_corpo)
verificar "2. Quitação — 400 PAYLOAD_INVALIDO (itens vazio)" 400 "$STATUS" "$CORPO" '"codigo":"PAYLOAD_INVALIDO"'

# --- 3. Quitação — 409 VENDA_JA_CANCELADA -----------------------------------
# Precondição via API (sem seed de banco): cancelar venda desconhecida cria
# já Cancelada (D-08, 200), então tentar quitar essa mesma venda dá 409.
VENDA_CANC_P_QUIT="V-SMOKE-JACANC-$TS"
chamar POST /cancelamento "{\"vendaId\":\"$VENDA_CANC_P_QUIT\",\"motivo\":\"Seed via API para cenario 409 (T-34)\"}" > /dev/null
RESP=$(chamar POST /quitacao "{\"vendaId\":\"$VENDA_CANC_P_QUIT\",\"clienteId\":\"C-SMOKE-1\",\"valorTotal\":10.00,\"itens\":[{\"produtoId\":\"P-01\",\"quantidade\":1,\"precoUnitario\":10.00}]}")
STATUS=$(echo "$RESP" | extrair_status); CORPO=$(echo "$RESP" | extrair_corpo)
verificar "3. Quitação — 409 VENDA_JA_CANCELADA" 409 "$STATUS" "$CORPO" '"codigo":"VENDA_JA_CANCELADA"'

# --- 4. Cancelamento — caminho feliz (venda desconhecida, D-08, 200) -------
VENDA_CANC_FELIZ="V-SMOKE-CANC-$TS"
RESP=$(chamar POST /cancelamento "{\"vendaId\":\"$VENDA_CANC_FELIZ\"}")
STATUS=$(echo "$RESP" | extrair_status); CORPO=$(echo "$RESP" | extrair_corpo)
verificar "4. Cancelamento — caminho feliz (200 Cancelada, D-08)" 200 "$STATUS" "$CORPO" '"status":"Cancelada"'

# --- 5. Cancelamento — 409 MOTIVO_OBRIGATORIO -------------------------------
# Precondição via API: quitar venda nova, depois cancelar sem motivo.
VENDA_CANC_MOTIVO="V-SMOKE-MOTIVO-$TS"
chamar POST /quitacao "{\"vendaId\":\"$VENDA_CANC_MOTIVO\",\"clienteId\":\"C-SMOKE-1\",\"valorTotal\":80.00,\"itens\":[{\"produtoId\":\"P-01\",\"quantidade\":1,\"precoUnitario\":80.00}]}" > /dev/null
RESP=$(chamar POST /cancelamento "{\"vendaId\":\"$VENDA_CANC_MOTIVO\"}")
STATUS=$(echo "$RESP" | extrair_status); CORPO=$(echo "$RESP" | extrair_corpo)
verificar "5. Cancelamento — 409 MOTIVO_OBRIGATORIO (Quitada sem motivo)" 409 "$STATUS" "$CORPO" '"codigo":"MOTIVO_OBRIGATORIO"'

# --- 6. Status — caminho feliz -----------------------------------------------
RESP=$(chamar GET "/$VENDA_QUIT/status")
STATUS=$(echo "$RESP" | extrair_status); CORPO=$(echo "$RESP" | extrair_corpo)
verificar "6. Status — caminho feliz (200 Quitada)" 200 "$STATUS" "$CORPO" '"status":"Quitada"'

# --- 7. Status — 404 VENDA_NAO_ENCONTRADA ------------------------------------
RESP=$(chamar GET "/V-SMOKE-NAO-EXISTE-$TS/status")
STATUS=$(echo "$RESP" | extrair_status); CORPO=$(echo "$RESP" | extrair_corpo)
verificar "7. Status — 404 VENDA_NAO_ENCONTRADA" 404 "$STATUS" "$CORPO" '"codigo":"VENDA_NAO_ENCONTRADA"'

# --- 8. Quitação — 401 NAO_AUTORIZADO (X-Api-Key ausente, T-37) -------------
RESP=$(chamar_sem_chave POST /quitacao "{\"vendaId\":\"V-SMOKE-401-SEMCHAVE-$TS\",\"clienteId\":\"C-SMOKE-1\",\"valorTotal\":10.00,\"itens\":[{\"produtoId\":\"P-01\",\"quantidade\":1,\"precoUnitario\":10.00}]}")
STATUS=$(echo "$RESP" | extrair_status); CORPO=$(echo "$RESP" | extrair_corpo)
verificar "8. Quitação — 401 NAO_AUTORIZADO (X-Api-Key ausente)" 401 "$STATUS" "$CORPO" '"codigo":"NAO_AUTORIZADO"'

# --- 9. Quitação — 401 NAO_AUTORIZADO (X-Api-Key inválida, T-37) -----------
RESP=$(chamar_chave_invalida POST /quitacao "{\"vendaId\":\"V-SMOKE-401-CHAVEINVALIDA-$TS\",\"clienteId\":\"C-SMOKE-1\",\"valorTotal\":10.00,\"itens\":[{\"produtoId\":\"P-01\",\"quantidade\":1,\"precoUnitario\":10.00}]}")
STATUS=$(echo "$RESP" | extrair_status); CORPO=$(echo "$RESP" | extrair_corpo)
verificar "9. Quitação — 401 NAO_AUTORIZADO (X-Api-Key inválida)" 401 "$STATUS" "$CORPO" '"codigo":"NAO_AUTORIZADO"'

echo ""
echo "== Resultado: $((TOTAL - FALHAS))/$TOTAL cenários verdes =="
if [ "$FALHAS" -gt 0 ]; then
  echo "FALHOU: $FALHAS cenário(s) não bateram com o esperado."
  exit 1
fi
echo "100% verde."
exit 0
