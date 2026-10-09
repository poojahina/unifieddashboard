from __future__ import annotations

import json
import uuid
from collections import defaultdict
from datetime import date, datetime, timedelta, timezone
from decimal import Decimal
from pathlib import Path
from typing import Any
from urllib.parse import urlparse

from fastapi import FastAPI, Query, Request, Response
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel, Field


app = FastAPI(
    title="Unified AI Consumption Dashboard",
    version="v1",
    description="Python/FastAPI demo backend with fixed fixtures and runtime-only integrations.",
    docs_url="/swagger",
    openapi_url="/swagger/v1/swagger.json",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:4200"],
    allow_methods=["*"],
    allow_headers=["*"],
)


class ApiError(Exception):
    def __init__(self, status_code: int, message: str):
        self.status_code = status_code
        self.message = message


def pascal(obj: Any) -> Any:
    if isinstance(obj, BaseModel):
        return pascal(obj.model_dump())
    if isinstance(obj, list):
        return [pascal(x) for x in obj]
    if isinstance(obj, dict):
        return {k[:1].upper() + k[1:]: pascal(v) for k, v in obj.items()}
    if isinstance(obj, Decimal):
        return float(obj)
    if isinstance(obj, (date, datetime, uuid.UUID)):
        return str(obj)
    return obj


@app.middleware("http")
async def response_headers(request: Request, call_next):
    correlation_id = uuid.uuid4().hex
    try:
        response = await call_next(request)
    except ApiError as exc:
        response = JSONResponse(
            status_code=exc.status_code,
            content={"Title": exc.message, "Status": exc.status_code, "CorrelationId": correlation_id},
        )
    response.headers["X-Correlation-ID"] = correlation_id
    response.headers["X-Content-Type-Options"] = "nosniff"
    response.headers["X-Frame-Options"] = "DENY"
    response.headers["Referrer-Policy"] = "no-referrer"
    response.headers["Cache-Control"] = "no-store"
    return response


class IntegrationRequest(BaseModel):
    providerType: str
    integrationName: str
    environment: str
    configuration: dict[str, str] = Field(default_factory=dict)
    team: str = "Engineering"
    enabled: bool = True


class ToggleRequest(BaseModel):
    enabled: bool


ACCOUNTS = [
    {"Id": "claude-prod", "Name": "Enterprise-Claude-Prod", "Provider": "Claude", "Environment": "Production", "Team": "Engineering", "Model": "Claude Sonnet"},
    {"Id": "claude-dev", "Name": "Claude-Development", "Provider": "Claude", "Environment": "Development", "Team": "AI COE", "Model": "Claude Opus"},
    {"Id": "openai-prod", "Name": "OpenAI-Production", "Provider": "OpenAI", "Environment": "Production", "Team": "Customer Support", "Model": "GPT-4.1"},
    {"Id": "openai-dev", "Name": "OpenAI-Development", "Provider": "OpenAI", "Environment": "Test", "Team": "Sales", "Model": "GPT-4.1 mini"},
    {"Id": "copilot-corp", "Name": "Corporate-Copilot", "Provider": "Copilot", "Environment": "Production", "Team": "Engineering", "Model": "Microsoft 365 Copilot"},
    {"Id": "servicenow-prod", "Name": "ServiceNow-Production", "Provider": "ServiceNow", "Environment": "Production", "Team": "Operations", "Model": "Now Assist"},
]
ACCOUNT_BY_ID = {a["Id"]: a for a in ACCOUNTS}
DEFAULT_FROM = date(2026, 8, 12)
DEFAULT_TO = date(2026, 9, 10)


RAW_ROWS = [
    ("2026-08-01","claude-prod",310200,6210000,2310000,1180.20,1850), ("2026-08-01","claude-dev",72700,1390000,550000,259.80,520),
    ("2026-08-01","openai-prod",291400,5200000,2080000,1080.10,2610), ("2026-08-01","openai-dev",51800,960000,320000,190.15,490),
    ("2026-08-01","copilot-corp",253900,4370000,1750000,845.60,3070), ("2026-08-01","servicenow-prod",129200,2070000,830000,484.25,1990),
    ("2026-08-12","claude-prod",15120,302400,113400,53.80,91), ("2026-08-12","openai-prod",13240,238320,92700,46.60,119), ("2026-08-12","copilot-corp",9840,167280,68880,31.20,118), ("2026-08-12","servicenow-prod",5020,80320,32630,17.80,75),
    ("2026-08-13","claude-prod",16280,325600,122100,57.90,97), ("2026-08-13","openai-prod",14450,260100,101150,50.80,130), ("2026-08-13","copilot-corp",10650,181050,74550,33.70,128), ("2026-08-13","servicenow-prod",5410,86560,35165,19.20,81),
    ("2026-08-14","claude-dev",15400,308000,115500,54.70,92), ("2026-08-14","openai-dev",13820,248760,96740,48.60,124), ("2026-08-14","copilot-corp",10120,172040,70840,32.10,121), ("2026-08-14","servicenow-prod",5100,81600,33150,18.10,76),
    ("2026-08-15","claude-prod",8020,160400,60150,28.50,48), ("2026-08-15","openai-prod",7340,132120,51380,25.80,66), ("2026-08-15","copilot-corp",5200,88400,36400,16.50,62), ("2026-08-15","servicenow-prod",3010,48160,19565,10.70,45),
    ("2026-08-16","claude-prod",7440,148800,55800,26.40,44), ("2026-08-16","openai-prod",6910,124380,48370,24.30,62), ("2026-08-16","copilot-corp",4850,82450,33950,15.40,58), ("2026-08-16","servicenow-prod",2780,44480,18070,9.90,42),
    ("2026-08-17","claude-prod",17620,352400,132150,62.60,106), ("2026-08-17","openai-prod",15200,273600,106400,53.40,137), ("2026-08-17","copilot-corp",11200,190400,78400,35.50,134), ("2026-08-17","servicenow-prod",5900,94400,38350,20.90,88),
    ("2026-08-18","claude-prod",18240,364800,136800,64.80,109), ("2026-08-18","openai-prod",15940,286920,111580,56.00,143), ("2026-08-18","copilot-corp",11640,197880,81480,36.90,140), ("2026-08-18","servicenow-prod",6120,97920,39780,21.70,92),
    ("2026-08-19","claude-dev",17100,342000,128250,60.80,103), ("2026-08-19","openai-dev",15120,272160,105840,53.10,136), ("2026-08-19","copilot-corp",11110,188870,77770,35.20,133), ("2026-08-19","servicenow-prod",5680,90880,36920,20.10,85),
    ("2026-08-20","claude-prod",18840,376800,141300,66.90,113), ("2026-08-20","openai-prod",16720,300960,117040,58.80,150), ("2026-08-20","copilot-corp",12400,210800,86800,39.30,149), ("2026-08-20","servicenow-prod",6400,102400,41600,22.70,96),
    ("2026-08-21","claude-prod",16900,338000,126750,60.00,101), ("2026-08-21","openai-prod",14960,269280,104720,52.60,135), ("2026-08-21","copilot-corp",10820,183940,75740,34.30,130), ("2026-08-21","servicenow-prod",5600,89600,36400,19.90,84),
    ("2026-08-22","claude-prod",9010,180200,67575,32.00,54), ("2026-08-22","openai-prod",8230,148140,57610,28.90,74), ("2026-08-22","copilot-corp",6040,102680,42280,19.10,72), ("2026-08-22","servicenow-prod",3320,53120,21580,11.80,50),
    ("2026-08-23","claude-dev",8480,169600,63600,30.10,51), ("2026-08-23","openai-dev",7680,138240,53760,27.00,69), ("2026-08-23","copilot-corp",5590,95030,39130,17.70,67), ("2026-08-23","servicenow-prod",3190,51040,20735,11.30,48),
    ("2026-08-24","claude-prod",19700,394000,147750,70.00,118), ("2026-08-24","openai-prod",17180,309240,120260,60.40,155), ("2026-08-24","copilot-corp",12800,217600,89600,40.50,154), ("2026-08-24","servicenow-prod",6560,104960,42640,23.30,98),
    ("2026-08-25","claude-prod",21200,424000,159000,75.30,127), ("2026-08-25","openai-prod",18340,330120,128380,64.50,165), ("2026-08-25","copilot-corp",13640,231880,95480,43.20,164), ("2026-08-25","servicenow-prod",7020,112320,45630,24.90,105),
    ("2026-08-26","claude-prod",20360,407200,152700,72.30,122), ("2026-08-26","openai-prod",17910,322380,125370,63.00,161), ("2026-08-26","copilot-corp",13080,222360,91560,41.40,157), ("2026-08-26","servicenow-prod",6780,108480,44070,24.00,102),
    ("2026-08-27","claude-dev",18280,365600,137100,64.90,110), ("2026-08-27","openai-dev",16100,289800,112700,56.60,145), ("2026-08-27","copilot-corp",11950,203150,83650,37.90,143), ("2026-08-27","servicenow-prod",6110,97760,39715,21.70,92),
    ("2026-08-28","claude-prod",17850,357000,133875,63.40,107), ("2026-08-28","openai-prod",15840,285120,110880,55.70,143), ("2026-08-28","copilot-corp",11540,196180,80780,36.50,138), ("2026-08-28","servicenow-prod",5980,95680,38870,21.20,90),
    ("2026-08-29","claude-prod",9840,196800,73800,34.90,59), ("2026-08-29","openai-prod",8720,156960,61040,30.70,78), ("2026-08-29","copilot-corp",6480,110160,45360,20.50,78), ("2026-08-29","servicenow-prod",3540,56640,23010,12.60,53),
    ("2026-08-30","claude-prod",9160,183200,68700,32.50,55), ("2026-08-30","openai-prod",8110,145980,56770,28.50,73), ("2026-08-30","copilot-corp",5970,101490,41790,18.90,72), ("2026-08-30","servicenow-prod",3280,52480,21320,11.60,49),
    ("2026-08-31","claude-dev",22100,442000,165750,78.50,133), ("2026-08-31","openai-dev",19280,347040,134960,67.80,174), ("2026-08-31","copilot-corp",14210,241570,99470,45.00,170), ("2026-08-31","servicenow-prod",7240,115840,47060,25.70,109),
    ("2026-09-01","claude-prod",23480,469600,176100,83.40,141), ("2026-09-01","openai-prod",20510,369180,143570,72.10,185), ("2026-09-01","copilot-corp",15120,257040,105840,47.90,181), ("2026-09-01","servicenow-prod",7810,124960,50765,27.70,117),
    ("2026-09-02","claude-prod",21560,431200,161700,76.60,129), ("2026-09-02","openai-prod",18940,340920,132580,66.60,170), ("2026-09-02","copilot-corp",13950,237150,97650,44.20,167), ("2026-09-02","servicenow-prod",7100,113600,46150,25.20,107),
    ("2026-09-03","claude-prod",20840,416800,156300,74.00,125), ("2026-09-03","openai-prod",18260,328680,127820,64.20,164), ("2026-09-03","copilot-corp",13400,227800,93800,42.50,161), ("2026-09-03","servicenow-prod",6880,110080,44720,24.40,103),
    ("2026-09-04","claude-dev",19200,384000,144000,68.20,115), ("2026-09-04","openai-dev",16980,305640,118860,59.70,153), ("2026-09-04","copilot-corp",12540,213180,87780,39.70,150), ("2026-09-04","servicenow-prod",6410,102560,41665,22.70,96),
    ("2026-09-05","claude-prod",10420,208400,78150,37.00,63), ("2026-09-05","openai-prod",9320,167760,65240,32.80,84), ("2026-09-05","copilot-corp",6820,115940,47740,21.60,82), ("2026-09-05","servicenow-prod",3710,59360,24115,13.20,56),
    ("2026-09-06","claude-prod",9820,196400,73650,34.90,59), ("2026-09-06","openai-prod",8740,157320,61180,30.70,79), ("2026-09-06","copilot-corp",6310,107270,44170,20.00,76), ("2026-09-06","servicenow-prod",3490,55840,22685,12.40,52),
    ("2026-09-07","claude-prod",24280,485600,182100,86.20,146), ("2026-09-07","openai-prod",21400,385200,149800,75.20,193), ("2026-09-07","copilot-corp",15660,266220,109620,49.60,188), ("2026-09-07","servicenow-prod",7990,127840,51935,28.30,120),
    ("2026-09-08","claude-dev",22640,452800,169800,80.40,136), ("2026-09-08","openai-dev",19820,356760,138740,69.70,178), ("2026-09-08","copilot-corp",14540,247180,101780,46.10,174), ("2026-09-08","servicenow-prod",7480,119680,48620,26.50,112),
    ("2026-09-09","claude-prod",23500,470000,176250,83.40,141), ("2026-09-09","openai-prod",20600,370800,144200,72.40,185), ("2026-09-09","copilot-corp",15050,255850,105350,47.70,181), ("2026-09-09","servicenow-prod",7700,123200,50050,27.30,116),
    ("2026-09-10","claude-prod",24840,496800,186300,88.20,149), ("2026-09-10","openai-prod",21820,392760,152740,76.70,196), ("2026-09-10","copilot-corp",16100,273700,112700,51.00,193), ("2026-09-10","servicenow-prod",8260,132160,53690,29.30,248),
]


def usage_record(row: tuple) -> dict[str, Any]:
    row_date, account_id, requests, input_tokens, output_tokens, cost, errors = row
    account = ACCOUNT_BY_ID[account_id]
    total = input_tokens + output_tokens
    success = requests - errors
    return {
        "Id": f"{row_date}/{account_id}", "Date": date.fromisoformat(row_date), "Provider": account["Provider"],
        "AccountId": account_id, "AccountName": account["Name"], "Environment": account["Environment"], "Team": account["Team"], "Model": account["Model"],
        "RequestCount": requests, "InputTokens": input_tokens, "OutputTokens": output_tokens, "Cost": Decimal(str(cost)),
        "FailedRequests": errors, "TotalTokens": total, "SuccessfulRequests": success, "SuccessRate": 0 if requests == 0 else 100 * success / requests,
        "Currency": "USD", "CostType": "EstimatedCost",
    }


USAGE = [usage_record(row) for row in RAW_ROWS]


def field(name, label, type_="text", required=False, secret=False, options=None, hint=None, when=None, values=None):
    return {"Name": name, "Label": label, "Type": type_, "Required": required, "Secret": secret, "Options": options, "Hint": hint, "RequiredWhenField": when, "RequiredWhenValues": values}


def cap():
    return {"requests": "supported", "inputTokens": "supported", "outputTokens": "supported", "totalTokens": "supported", "cost": "supported", "models": "supported", "accounts": "supported", "errors": "supported", "users": "unsupported"}


PROVIDERS = [
    {"ProviderType": "Claude", "DisplayName": "Anthropic Claude", "Color": "#b48768", "Monogram": "A", "Description": "Enterprise language models", "Capabilities": cap(), "Fields": [field("adminApiKey", "Admin API key", "password", True, True, hint="Use any dummy value. invalid-key simulates authentication failure."), field("organizationId", "Organization ID"), field("baseEndpoint", "Base endpoint", "url", hint="Optional HTTPS endpoint. No external calls are made.")]},
    {"ProviderType": "OpenAI", "DisplayName": "OpenAI", "Color": "#438a78", "Monogram": "O", "Description": "Models and API platform", "Capabilities": cap(), "Fields": [field("adminApiKey", "Admin API key / API key", "password", True, True), field("organizationId", "Organization ID"), field("accountId", "Account ID"), field("baseEndpoint", "Base endpoint", "url")]},
    {"ProviderType": "Copilot", "DisplayName": "Microsoft Copilot", "Color": "#637dc5", "Monogram": "M", "Description": "Workplace AI assistance", "Capabilities": cap(), "Fields": [field("tenantId", "Tenant ID", required=True), field("clientId", "Client ID", required=True), field("clientSecret", "Client secret", "password", True, True), field("baseEndpoint", "Base endpoint", "url")]},
    {"ProviderType": "ServiceNow", "DisplayName": "ServiceNow", "Color": "#819866", "Monogram": "S", "Description": "Enterprise workflow intelligence", "Capabilities": cap(), "Fields": [field("baseEndpoint", "Instance URL", "url", True), field("authenticationType", "Authentication type", "dropdown", True, options=["Basic", "OAuth2"]), field("username", "Username", when="authenticationType", values=["Basic"]), field("password", "Password", "password", secret=True, when="authenticationType", values=["Basic"]), field("clientId", "Client ID", when="authenticationType", values=["OAuth2"]), field("clientSecret", "Client secret", "password", secret=True, when="authenticationType", values=["OAuth2"])]},
    {"ProviderType": "CustomREST", "DisplayName": "Custom REST API", "Color": "#9279b0", "Monogram": "R", "Description": "Configure another AI or SaaS provider", "Capabilities": cap(), "Fields": [field("providerName", "Provider name", required=True), field("baseEndpoint", "Base endpoint", "url", True), field("usageEndpoint", "Usage endpoint path", required=True, hint="For example /v1/usage. Configuration only in this POC."), field("authenticationType", "Authentication type", "dropdown", True, options=["ApiKey", "Bearer", "Basic", "OAuth2", "CustomHeader"]), field("apiKey", "API key / admin key / token", "password", secret=True, when="authenticationType", values=["ApiKey", "Bearer", "CustomHeader"]), field("authorizationHeader", "Authorization header", when="authenticationType", values=["ApiKey", "CustomHeader"]), field("authorizationScheme", "Authorization scheme"), field("username", "Username", when="authenticationType", values=["Basic"]), field("password", "Password", "password", secret=True, when="authenticationType", values=["Basic"]), field("clientId", "Client ID", when="authenticationType", values=["OAuth2"]), field("clientSecret", "Client secret", "password", secret=True, when="authenticationType", values=["OAuth2"]), field("tokenEndpoint", "Token endpoint", "url", when="authenticationType", values=["OAuth2"]), field("region", "Region"), field("httpMethod", "HTTP method", "dropdown", True, options=["GET", "POST"]), field("customHeaders", "Custom headers (JSON)", "textarea", secret=True), field("queryParameters", "Query parameters (JSON)", "textarea", secret=True), field("responseMapping", "Response mapping (JSON)", "textarea", True)]},
]


def provider(provider_type: str) -> dict[str, Any]:
    for item in PROVIDERS:
        if item["ProviderType"].lower() == provider_type.lower():
            return item
    raise ApiError(400, "Unknown provider type.")


def parse_period(from_: str | None, to: str | None) -> tuple[date, date]:
    start = date.fromisoformat(from_) if from_ else DEFAULT_FROM
    end = date.fromisoformat(to) if to else DEFAULT_TO
    if start > end or (end - start).days > 366:
        raise ApiError(400, "Select a valid date range of at most 366 days.")
    return start, end


def filtered(params: dict[str, Any]) -> list[dict[str, Any]]:
    start, end = parse_period(params.get("from"), params.get("to"))
    rows = [r for r in USAGE if start <= r["Date"] <= end]
    for query_name, row_name in [("provider", "Provider"), ("account", "AccountId"), ("environment", "Environment"), ("team", "Team"), ("model", "Model")]:
        if params.get(query_name):
            rows = [r for r in rows if r[row_name] == params[query_name]]
    return rows


def sum_nullable(rows: list[dict[str, Any]], key: str):
    return sum((r[key] for r in rows if r[key] is not None), 0) if any(r[key] is not None for r in rows) else None


def change(current, previous):
    return None if previous == 0 else round(float((current - previous) / previous * 100), 2)


def previous_rows(params: dict[str, Any]) -> list[dict[str, Any]]:
    start, end = parse_period(params.get("from"), params.get("to"))
    days = (end - start).days + 1
    copy = dict(params)
    copy["from"] = (start - timedelta(days=days)).isoformat()
    copy["to"] = (start - timedelta(days=1)).isoformat()
    return filtered(copy)


@app.get("/api/metadata")
def metadata():
    return pascal({"isDemoMode": True, "defaultFrom": DEFAULT_FROM, "defaultTo": DEFAULT_TO, "accounts": ACCOUNTS, "environments": sorted({a["Environment"] for a in ACCOUNTS}), "teams": sorted({a["Team"] for a in ACCOUNTS}), "models": sorted({a["Model"] for a in ACCOUNTS}), "comparisonNote": "Previous-period comparisons use fixed monthly baseline fixtures; custom periods may have no comparable baseline."})


@app.get("/api/dashboard/summary")
def summary(request: Request):
    params = dict(request.query_params)
    rows = filtered(params)
    prior = previous_rows(params)
    requests = sum(r["RequestCount"] for r in rows)
    failures = sum(r["FailedRequests"] for r in rows)
    prior_requests = sum(r["RequestCount"] for r in prior)
    success = 0 if requests == 0 else 100 * (requests - failures) / requests
    prior_success = 0 if prior_requests == 0 else 100 * sum(r["SuccessfulRequests"] for r in prior) / prior_requests
    return pascal({"totalRequests": requests, "inputTokens": sum_nullable(rows, "InputTokens"), "outputTokens": sum_nullable(rows, "OutputTokens"), "totalTokens": sum_nullable(rows, "TotalTokens"), "estimatedCost": sum_nullable(rows, "Cost"), "successRate": success, "activeProviders": len({r["Provider"] for r in rows}), "connectedAccounts": len({r["AccountId"] for r in rows}), "requestChangePercentage": change(requests, prior_requests), "tokenChangePercentage": change(sum_nullable(rows, "TotalTokens") or 0, sum_nullable(prior, "TotalTokens") or 0), "costChangePercentage": change(sum_nullable(rows, "Cost") or Decimal(0), sum_nullable(prior, "Cost") or Decimal(0)), "errorRate": 0 if requests == 0 else 100 * failures / requests, "inputChangePercentage": change(sum_nullable(rows, "InputTokens") or 0, sum_nullable(prior, "InputTokens") or 0), "outputChangePercentage": change(sum_nullable(rows, "OutputTokens") or 0, sum_nullable(prior, "OutputTokens") or 0), "providerChangePercentage": change(len({r["Provider"] for r in rows}), len({r["Provider"] for r in prior})), "accountChangePercentage": change(len({r["AccountId"] for r in rows}), len({r["AccountId"] for r in prior})), "successChangePercentage": change(success, prior_success)})


def provider_summary_row(provider_name: str, rows: list[dict[str, Any]]) -> dict[str, Any]:
    p = provider(provider_name)
    requests = sum(r["RequestCount"] for r in rows)
    failures = sum(r["FailedRequests"] for r in rows)
    return {"Provider": p["ProviderType"], "DisplayName": p["DisplayName"], "Color": p["Color"], "Monogram": p["Monogram"], "Status": "Demo connected", "Health": "Warning" if p["ProviderType"] == "ServiceNow" else "Healthy", "Requests": requests, "InputTokens": sum_nullable(rows, "InputTokens"), "OutputTokens": sum_nullable(rows, "OutputTokens"), "TotalTokens": sum_nullable(rows, "TotalTokens"), "EstimatedCost": sum_nullable(rows, "Cost"), "SuccessRate": 0 if requests == 0 else 100 * (requests - failures) / requests, "FailedRequests": failures, "ConnectedAccounts": len({r["AccountId"] for r in rows}), "LastSync": datetime(2026, 9, 10, 8, 42, tzinfo=timezone.utc)}


@app.get("/api/dashboard/providers")
@app.get("/api/dashboard/provider-summary")
def provider_summaries(request: Request):
    groups = defaultdict(list)
    for row in filtered(dict(request.query_params)):
        groups[row["Provider"]].append(row)
    return pascal([provider_summary_row(name, rows) for name, rows in groups.items()])


def group_consumers(rows: list[dict[str, Any]], key: str):
    groups = defaultdict(list)
    for row in rows:
        groups[row[key]].append(row)
    return sorted([{"Name": k, "Requests": sum(r["RequestCount"] for r in v), "Tokens": sum_nullable(v, "TotalTokens"), "Cost": sum_nullable(v, "Cost")} for k, v in groups.items()], key=lambda x: x["Cost"] or 0, reverse=True)


@app.get("/api/dashboard/top-consumers")
def top_consumers(request: Request):
    rows = filtered(dict(request.query_params))
    return pascal({"accounts": group_consumers(rows, "AccountName"), "models": group_consumers(rows, "Model"), "teams": group_consumers(rows, "Team"), "environments": group_consumers(rows, "Environment")})


@app.get("/api/usage")
def usage(request: Request, page: int = 1, pageSize: int = 25, sortBy: str = "date", sortDirection: str = "desc"):
    if page < 1 or pageSize < 1 or pageSize > 200:
        raise ApiError(400, "Page must be positive; pageSize must be between 1 and 200.")
    sort_keys = {"date": "Date", "provider": "Provider", "requests": "RequestCount", "cost": "Cost"}
    if sortBy not in sort_keys:
        raise ApiError(400, "Unsupported sort field.")
    if sortDirection not in {"asc", "desc"}:
        raise ApiError(400, "Sort direction must be asc or desc.")
    rows = sorted(filtered(dict(request.query_params)), key=lambda r: (r[sort_keys[sortBy]], r["Id"]), reverse=sortDirection == "desc")
    start = (page - 1) * pageSize
    return pascal({"items": rows[start:start + pageSize], "total": len(rows), "page": page, "pageSize": pageSize})


def metric_value(row: dict[str, Any], metric: str):
    mapping = {"requests": "RequestCount", "inputTokens": "InputTokens", "outputTokens": "OutputTokens", "totalTokens": "TotalTokens", "cost": "Cost", "errors": "FailedRequests"}
    if metric not in mapping:
        raise ApiError(400, "Unsupported metric.")
    return row[mapping[metric]]


@app.get("/api/usage/trends")
@app.get("/api/costs/trends")
def trends(request: Request, metric: str = "requests", granularity: str = "daily"):
    if request.url.path == "/api/costs/trends":
        metric = "cost"
    if granularity not in {"daily", "weekly", "monthly"}:
        raise ApiError(400, "Choose daily, weekly or monthly granularity.")
    def bucket(d: date):
        return d - timedelta(days=d.weekday()) if granularity == "weekly" else d.replace(day=1) if granularity == "monthly" else d
    groups = defaultdict(list)
    for row in filtered(dict(request.query_params)):
        groups[(bucket(row["Date"]), row["Provider"])].append(row)
    result = [{"Date": k[0], "Provider": k[1], "Value": sum((metric_value(r, metric) for r in v if metric_value(r, metric) is not None), Decimal(0)) if any(metric_value(r, metric) is not None for r in v) else None} for k, v in groups.items()]
    return pascal(sorted(result, key=lambda x: x["Date"]))


@app.get("/api/usage/token-trends")
def token_trends(request: Request):
    groups = defaultdict(list)
    for row in filtered(dict(request.query_params)):
        groups[(row["Date"], row["Provider"])].append(row)
    return pascal([{"date": d, "provider": p, "inputTokens": sum(r["InputTokens"] for r in rows), "outputTokens": sum(r["OutputTokens"] for r in rows), "totalTokens": sum(r["TotalTokens"] for r in rows)} for (d, p), rows in groups.items()])


@app.get("/api/usage/request-status")
def request_status(request: Request):
    groups = defaultdict(list)
    for row in filtered(dict(request.query_params)):
        groups[row["Provider"]].append(row)
    return pascal([{"provider": p, "successfulRequests": sum(r["SuccessfulRequests"] for r in rows), "failedRequests": sum(r["FailedRequests"] for r in rows), "successRate": 100 * sum(r["SuccessfulRequests"] for r in rows) / sum(r["RequestCount"] for r in rows)} for p, rows in groups.items()])


@app.get("/api/costs/summary")
def costs(request: Request):
    params = dict(request.query_params)
    s = summary(request)
    top = top_consumers(request)
    previous = sum_nullable(previous_rows(params), "Cost")
    return pascal({"totalCost": s["EstimatedCost"], "previousPeriodCost": previous, "percentageChange": s["CostChangePercentage"], "costByProvider": [{"Name": x["DisplayName"], "Cost": x["EstimatedCost"], "Requests": x["Requests"]} for x in provider_summaries(request)], "costByAccount": top["Accounts"], "costByModel": top["Models"], "costByTeam": top["Teams"]})


@app.get("/api/health")
def health():
    return {"Status": "Healthy", "Mode": "Demo", "Database": "Not used", "Persistence": "None", "ExternalProviderCalls": False}


@app.get("/api/alerts")
def alerts():
    return pascal([
        {"id": "service-now-errors", "severity": "Warning", "title": "ServiceNow error rate increased", "description": "The fixed September 10 demo observation shows a 3.0% error rate. Review request outcomes in Usage Analytics.", "provider": "ServiceNow", "timestamp": datetime(2026, 9, 10, 8, 40, tzinfo=timezone.utc)},
        {"id": "cost-growth", "severity": "Info", "title": "Consumption is trending upward", "description": "Weekday demand is increasing in the fixed demo dataset. Compare team and account allocation in Cost Analytics.", "provider": "All providers", "timestamp": datetime(2026, 9, 10, 8, tzinfo=timezone.utc)},
    ])


@app.get("/api/providers")
def providers():
    return PROVIDERS


@app.get("/api/providers/{provider_type}")
@app.get("/api/providers/{provider_type}/configuration-schema")
def provider_schema(provider_type: str):
    return provider(provider_type)


ADDED: dict[str, dict[str, Any]] = {}
SECRETS: dict[str, dict[str, str]] = {}
DEFAULT_CHANGES: dict[str, dict[str, Any] | None] = {}
AUDIT = [
    {"Id": "20000000-0000-0000-0000-000000000001", "Timestamp": datetime(2026, 9, 10, 8, 42, tzinfo=timezone.utc), "User": "Demo Administrator", "Action": "Sync completed", "Provider": "Claude", "Integration": "Enterprise-Claude-Prod", "Result": "Completed", "CorrelationId": "demo-sync-claude"},
    {"Id": "20000000-0000-0000-0000-000000000002", "Timestamp": datetime(2026, 9, 10, 8, 40, tzinfo=timezone.utc), "User": "System", "Action": "Elevated error rate", "Provider": "ServiceNow", "Integration": "ServiceNow-Production", "Result": "Warning", "CorrelationId": "demo-health-servicenow"},
    {"Id": "20000000-0000-0000-0000-000000000003", "Timestamp": datetime(2026, 9, 10, 8, 38, tzinfo=timezone.utc), "User": "Demo Administrator", "Action": "Connection tested", "Provider": "OpenAI", "Integration": "OpenAI-Production", "Result": "Connected", "CorrelationId": "demo-test-openai"},
]
HISTORY: list[dict[str, Any]] = []


def defaults():
    rows = []
    for index, a in enumerate(ACCOUNTS, start=1):
        iid = f"10000000-0000-0000-0000-{index:012d}"
        row = {"Id": iid, "ProviderType": a["Provider"], "IntegrationName": a["Name"], "Account": a["Name"], "Environment": a["Environment"], "Team": a["Team"], "Status": "Connected", "Health": "Warning" if a["Provider"] == "ServiceNow" else "Healthy", "Enabled": True, "LastSync": datetime(2026, 9, 10, 8, 42, tzinfo=timezone.utc), "LastSuccessfulSync": datetime(2026, 9, 10, 8, 42, tzinfo=timezone.utc), "CreatedBy": "Demo Administrator", "IsDefault": True, "IsDemoMode": True, "HasCredentials": False, "Configuration": {}}
        changed = DEFAULT_CHANGES.get(iid, row)
        if changed is not None:
            rows.append(changed)
    return rows


def current():
    return defaults() + list(ADDED.values())


def integration(iid: str):
    for row in current():
        if row["Id"] == iid:
            return dict(row)
    raise ApiError(404, "Integration not found.")


def test_config(provider_type: str, config: dict[str, str]):
    schema = provider(provider_type)
    field_names = {f["Name"] for f in schema["Fields"]}
    if len(config) > 40 or any(v is None or len(v) > 16000 for v in config.values()):
        raise ApiError(400, "Configuration is too large or contains invalid values.")
    if any(k not in field_names for k in config):
        raise ApiError(400, "Unknown configuration field.")
    for f in schema["Fields"]:
        value = (config.get(f["Name"]) or "").strip()
        required = f["Required"] or (f["RequiredWhenField"] and config.get(f["RequiredWhenField"]) in (f["RequiredWhenValues"] or []))
        if required and not value:
            return {"Success": False, "Status": "MissingCredentials", "Message": f'{f["Label"]} is required.', "ResponseTimeMs": 24, "IsDemoMode": True}
        if f["Type"] == "url" and value:
            parsed = urlparse(value)
            if parsed.scheme != "https" or not parsed.netloc or parsed.username or parsed.password or parsed.query or parsed.fragment:
                return {"Success": False, "Status": "InvalidEndpoint", "Message": "Use an HTTPS endpoint without user information, query parameters, or fragments. Put query parameters in the protected configuration field.", "ResponseTimeMs": 32, "IsDemoMode": True}
        if f["Options"] and value and value not in f["Options"]:
            return {"Success": False, "Status": "InvalidConfiguration", "Message": f'Select a valid {f["Label"]}.', "ResponseTimeMs": 25, "IsDemoMode": True}
        if f["Type"] == "textarea" and value:
            try:
                if not isinstance(json.loads(value), dict):
                    return {"Success": False, "Status": "InvalidConfiguration", "Message": f'{f["Label"]} must be a JSON object.', "ResponseTimeMs": 25, "IsDemoMode": True}
            except json.JSONDecodeError:
                return {"Success": False, "Status": "InvalidConfiguration", "Message": f'{f["Label"]} must contain valid JSON.', "ResponseTimeMs": 25, "IsDemoMode": True}
    for status, message in [("invalid-key", "The demo credential was rejected. Try a different dummy value."), ("rate-limited", "Simulated provider rate limit. Try another dummy credential."), ("unavailable", "The demo provider is temporarily unavailable.")]:
        if status in config.values():
            return {"Success": False, "Status": {"invalid-key": "AuthenticationFailed", "rate-limited": "RateLimited", "unavailable": "ProviderUnavailable"}[status], "Message": message, "ResponseTimeMs": 184, "IsDemoMode": True}
    return {"Success": True, "Status": "Connected", "Message": "Connection validated successfully in Demo Mode. No external request was made.", "ResponseTimeMs": 184, "IsDemoMode": True}


def audit(action: str, view: dict[str, Any], result: str, correlation: str):
    AUDIT.append({"Id": str(uuid.uuid4()), "Timestamp": datetime.now(timezone.utc), "User": "Demo Administrator", "Action": action, "Provider": view["ProviderType"], "Integration": view["IntegrationName"], "Result": result, "CorrelationId": correlation})
    del AUDIT[:-1000]


@app.get("/api/integrations")
def list_integrations():
    return pascal(current())


@app.get("/api/integrations/{iid}")
def get_integration(iid: str):
    return pascal(integration(iid))


@app.post("/api/integrations/test")
def test_integration(request: Request, body: IntegrationRequest):
    result = test_config(body.providerType, body.configuration)
    p = provider(body.providerType)
    AUDIT.append({"Id": str(uuid.uuid4()), "Timestamp": datetime.now(timezone.utc), "User": "Demo Administrator", "Action": "Connection tested", "Provider": p["ProviderType"], "Integration": "Unsaved integration", "Result": result["Status"], "CorrelationId": request.headers.get("X-Correlation-ID", "")})
    return result


def save_integration(body: IntegrationRequest, iid: str | None, correlation: str):
    schema = provider(body.providerType)
    name = (body.integrationName or "").strip()
    if not (1 <= len(name) <= 120) or any(ord(c) < 32 for c in name):
        raise ApiError(400, "Enter an integration name of 1-120 characters.")
    if body.environment not in {"Production", "Development", "Test", "Staging"}:
        raise ApiError(400, "Select a valid environment.")
    if not body.team.strip() or len(body.team) > 120:
        raise ApiError(400, "Enter a team of 1-120 characters.")
    old = integration(iid) if iid else None
    if old and old["ProviderType"] != schema["ProviderType"]:
        raise ApiError(400, "An existing integration's provider cannot be changed.")
    if any(x["Id"] != iid and x["IntegrationName"].lower() == name.lower() for x in current()):
        raise ApiError(409, "An integration with that name already exists.")
    config = dict(body.configuration)
    if iid in SECRETS:
        for key, value in SECRETS[iid].items():
            if not config.get(key):
                config[key] = value
    result = test_config(schema["ProviderType"], config)
    if not result["Success"]:
        raise ApiError(400, result["Message"])
    secret_names = {f["Name"] for f in schema["Fields"] if f["Secret"]}
    public = {k: v for k, v in config.items() if k not in secret_names}
    secrets = {k: v for k, v in config.items() if k in secret_names}
    vid = iid or str(uuid.uuid4())
    view = {"Id": vid, "ProviderType": schema["ProviderType"], "IntegrationName": name, "Account": name, "Environment": body.environment, "Team": body.team, "Status": "Connected" if body.enabled else "Disabled", "Health": "Healthy", "Enabled": body.enabled, "LastSync": old["LastSync"] if old else datetime.min.replace(tzinfo=timezone.utc), "LastSuccessfulSync": old["LastSuccessfulSync"] if old else None, "CreatedBy": old["CreatedBy"] if old else "Demo Administrator", "IsDefault": old["IsDefault"] if old else False, "IsDemoMode": True, "HasCredentials": bool(secrets), "Configuration": public}
    if old and old["IsDefault"]:
        DEFAULT_CHANGES[old["Id"]] = None
    ADDED[vid] = view
    SECRETS[vid] = secrets
    audit("Integration updated" if old else "Integration created", view, "Success", correlation)
    return view


@app.post("/api/integrations")
def add_integration(request: Request, response: Response, body: IntegrationRequest):
    response.status_code = 201
    return pascal(save_integration(body, None, request.headers.get("X-Correlation-ID", "")))


@app.put("/api/integrations/{iid}")
def edit_integration(request: Request, iid: str, body: IntegrationRequest):
    return pascal(save_integration(body, iid, request.headers.get("X-Correlation-ID", "")))


@app.post("/api/integrations/{iid}/test")
def test_existing(request: Request, iid: str):
    view = integration(iid)
    config = dict(view["Configuration"])
    config.update(SECRETS.get(iid, {}))
    result = {"Success": True, "Status": "Connected", "Message": "Default integration validated in Demo Mode.", "ResponseTimeMs": 184, "IsDemoMode": True} if view["IsDefault"] else test_config(view["ProviderType"], config)
    audit("Connection tested", view, result["Status"], request.headers.get("X-Correlation-ID", ""))
    return result


@app.post("/api/integrations/{iid}/sync")
def sync(request: Request, iid: str):
    view = integration(iid)
    if not view["Enabled"]:
        raise ApiError(409, "Enable this integration before synchronizing.")
    now = datetime.now(timezone.utc)
    result = {"IntegrationId": iid, "Success": True, "RecordsProcessed": 1250 if view["IsDefault"] else 0, "Status": "Completed", "Message": "Demo synchronization completed. The fixed historical dataset is unchanged.", "StartedAt": now, "CompletedAt": now}
    HISTORY.append(result)
    view.update({"LastSync": now, "LastSuccessfulSync": now, "Health": "Healthy"})
    if view["Id"] in ADDED:
        ADDED[view["Id"]] = view
    else:
        DEFAULT_CHANGES[view["Id"]] = view
    audit("Sync completed", view, "Completed", request.headers.get("X-Correlation-ID", ""))
    return pascal(result)


@app.get("/api/integrations/{iid}/sync-history")
def sync_history(iid: str):
    view = integration(iid)
    rows = [x for x in HISTORY if x["IntegrationId"] == iid]
    if view["IsDefault"]:
        rows.append({"IntegrationId": iid, "Success": True, "RecordsProcessed": 1250, "Status": "Completed", "Message": "Historical demo sync.", "StartedAt": datetime(2026, 9, 10, 8, 41, tzinfo=timezone.utc), "CompletedAt": datetime(2026, 9, 10, 8, 42, tzinfo=timezone.utc)})
    return pascal(sorted(rows, key=lambda x: x["CompletedAt"], reverse=True))


@app.patch("/api/integrations/{iid}/enabled")
def toggle(request: Request, iid: str, body: ToggleRequest):
    view = integration(iid)
    view["Enabled"] = body.enabled
    view["Status"] = "Connected" if body.enabled else "Disabled"
    if iid in ADDED:
        ADDED[iid] = view
    else:
        DEFAULT_CHANGES[iid] = view
    audit("Integration enabled" if body.enabled else "Integration disabled", view, "Success", request.headers.get("X-Correlation-ID", ""))
    return pascal(view)


@app.delete("/api/integrations/{iid}", status_code=204)
def delete_integration(request: Request, iid: str):
    view = integration(iid)
    ADDED.pop(iid, None)
    SECRETS.pop(iid, None)
    if view["IsDefault"]:
        DEFAULT_CHANGES[iid] = None
    audit("Integration deleted", view, "Success", request.headers.get("X-Correlation-ID", ""))


@app.get("/api/audit-logs")
def audit_logs(provider: str | None = None, action: str | None = None, result: str | None = None, user: str | None = None, from_: str | None = Query(None, alias="from"), to: str | None = None, page: int = 1, pageSize: int = 25):
    if page < 1 or pageSize < 1 or pageSize > 200:
        raise ApiError(400, "Invalid pagination.")
    rows = list(AUDIT)
    if provider:
        rows = [x for x in rows if x["Provider"] == provider]
    if action:
        rows = [x for x in rows if action.lower() in x["Action"].lower()]
    if result:
        rows = [x for x in rows if x["Result"] == result]
    if user:
        rows = [x for x in rows if user.lower() in x["User"].lower()]
    if from_:
        rows = [x for x in rows if x["Timestamp"].date() >= date.fromisoformat(from_)]
    if to:
        rows = [x for x in rows if x["Timestamp"].date() <= date.fromisoformat(to)]
    rows = sorted(rows, key=lambda x: x["Timestamp"], reverse=True)
    start = (page - 1) * pageSize
    return pascal({"items": rows[start:start + pageSize], "total": len(rows), "page": page, "pageSize": pageSize})


@app.post("/api/demo/normalize")
def demo_normalize(body: dict[str, Any]):
    return {"RawProvider": body.get("provider") or body.get("Provider"), "Normalized": []}


@app.post("/api/demo/provider/{provider_type}/normalize")
def demo_provider_normalize(provider_type: str):
    provider(provider_type)
    return {"RawProvider": provider_type, "Normalized": []}


frontend_dist = Path(__file__).resolve().parent.parent / "static"
if frontend_dist.exists():
    app.mount("/", StaticFiles(directory=frontend_dist, html=True), name="frontend")
