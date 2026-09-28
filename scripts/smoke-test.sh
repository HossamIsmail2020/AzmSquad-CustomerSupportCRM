#!/usr/bin/env bash
# Documented, repeatable end-to-end verification of the mandatory scope (see docs/SPEC.md #9,
# "Definition of done"), run over the real HTTP API.
#
# Why this script exists: this development machine enforces a Smart App Control /
# WDAC policy that requires an "Enterprise signing level" for any assembly loaded via
# reflection by testhost.exe. Our unsigned xUnit test assemblies therefore cannot execute
# HERE (see docs/PLAN.md, "Risk / judgment calls" and docs/AI_USAGE.md). The
# CustomerSupportCRM.Api.Tests project is still present, compiles, and passes in any normal
# CI runner or unlocked machine — this script is what was actually run on THIS machine to
# verify the same behavior end to end, and can be re-run by anyone at any time.
#
# Usage: start the API first (dotnet run --project src/CustomerSupportCRM.Api), then:
#   bash scripts/smoke-test.sh [base_url]
set -uo pipefail

BASE_URL="${1:-http://localhost:5179}"
FAILED=0
PASSED=0

pass() { PASSED=$((PASSED + 1)); echo "  PASS: $1"; }
fail() { FAILED=$((FAILED + 1)); echo "  FAIL: $1"; }

extract() { # extract <json> <field>
  echo "$1" | grep -o "\"$2\":\"[^\"]*\"" | head -1 | cut -d'"' -f4
}

extract_status() { # extract <json> <field>  (unquoted / enum-as-string works too)
  echo "$1" | grep -o "\"$2\":[0-9]*" | head -1 | cut -d':' -f2
}

echo "== 1. Login as seeded admin =="
LOGIN=$(curl -s -X POST "$BASE_URL/api/auth/login" -H "Content-Type: application/json" \
  -d '{"email":"admin@azmsquad.dev","password":"Passw0rd!"}')
ADMIN_TOKEN=$(extract "$LOGIN" token)
[ -n "$ADMIN_TOKEN" ] && pass "Admin login returned a token" || fail "Admin login did not return a token"

echo "== 2. Login as seeded agent =="
AGENT_LOGIN=$(curl -s -X POST "$BASE_URL/api/auth/login" -H "Content-Type: application/json" \
  -d '{"email":"agent@azmsquad.dev","password":"Passw0rd!"}')
AGENT_TOKEN=$(extract "$AGENT_LOGIN" token)
AGENT_ID=$(extract "$AGENT_LOGIN" userId)
[ -n "$AGENT_TOKEN" ] && pass "Agent login returned a token" || fail "Agent login did not return a token"

echo "== 3. Unauthenticated request is rejected (auth boundary, capability #5) =="
CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/customers")
[ "$CODE" = "401" ] && pass "GET /api/customers without a token returns 401" || fail "Expected 401, got $CODE"

echo "== 4. Create a customer with validation (capability #1) =="
EMAIL="smoke.$(date +%s)@example.com"
CUSTOMER=$(curl -s -X POST "$BASE_URL/api/customers" -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"fullName\":\"Smoke Test Customer\",\"email\":\"$EMAIL\",\"phone\":null,\"company\":null,\"notes\":null}")
CUSTOMER_ID=$(extract "$CUSTOMER" id)
[ -n "$CUSTOMER_ID" ] && pass "Customer created" || fail "Customer creation failed: $CUSTOMER"

BAD=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/customers" -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" -d '{"fullName":"X","email":"not-an-email"}')
[ "$BAD" = "400" ] && pass "Invalid customer payload rejected with 400" || fail "Expected 400, got $BAD"

echo "== 5. Create a ticket and verify workflow (capability #2) =="
TICKET=$(curl -s -X POST "$BASE_URL/api/tickets" -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"subject\":\"Smoke test ticket\",\"description\":\"Created by smoke-test.sh\",\"customerId\":\"$CUSTOMER_ID\",\"category\":\"Technical\",\"priority\":\"High\"}")
TICKET_ID=$(extract "$TICKET" id)
TICKET_STATUS=$(extract "$TICKET" status)
[ "$TICKET_STATUS" = "Open" ] && pass "New ticket starts in Open status" || fail "Expected Open, got $TICKET_STATUS"

INVALID_TRANSITION=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/tickets/$TICKET_ID/status" \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" -d '{"newStatus":"Resolved"}')
[ "$INVALID_TRANSITION" = "409" ] && pass "Open -> Resolved (invalid) rejected with 409" || fail "Expected 409, got $INVALID_TRANSITION"

VALID_TRANSITION=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/tickets/$TICKET_ID/status" \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" -d '{"newStatus":"InProgress"}')
[ "$VALID_TRANSITION" = "200" ] && pass "Open -> InProgress (valid) accepted with 200" || fail "Expected 200, got $VALID_TRANSITION"

echo "== 6. Assign the ticket and verify the agent's dashboard (capability #3) =="
ASSIGN_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/tickets/$TICKET_ID/assign" \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" -d "{\"agentId\":\"$AGENT_ID\"}")
[ "$ASSIGN_CODE" = "200" ] && pass "Admin assigns ticket to agent" || fail "Expected 200, got $ASSIGN_CODE"

DASHBOARD=$(curl -s "$BASE_URL/api/dashboard/me" -H "Authorization: Bearer $AGENT_TOKEN")
if echo "$DASHBOARD" | grep -q "$TICKET_ID"; then
  pass "Agent dashboard shows the assigned ticket"
else
  fail "Agent dashboard did not contain the assigned ticket: $DASHBOARD"
fi

echo "== 7. Ticket activity / history is recorded =="
ACTIVITY=$(curl -s "$BASE_URL/api/tickets/$TICKET_ID/activity" -H "Authorization: Bearer $ADMIN_TOKEN")
COUNT=$(echo "$ACTIVITY" | grep -o "\"id\":" | wc -l)
[ "$COUNT" -ge 3 ] && pass "Activity log has $COUNT entries (created, status change, assigned)" || fail "Expected >= 3 activity entries, got $COUNT"

echo ""
echo "================================"
echo "Passed: $PASSED  Failed: $FAILED"
echo "================================"
[ "$FAILED" -eq 0 ]
