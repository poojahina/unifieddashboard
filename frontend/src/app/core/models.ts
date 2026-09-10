export interface ProviderField { name:string; label:string; type:string; required:boolean; secret:boolean; options:string[]|null; hint:string|null; requiredWhenField:string|null; requiredWhenValues:string[]|null; }
export interface Provider { providerType:string; displayName:string; color:string; monogram:string; description:string; capabilities:Record<string,string>; fields:ProviderField[]; }
export interface Account { id:string; name:string; provider:string; environment:string; team:string; model:string; }
export interface Metadata { isDemoMode:boolean; defaultFrom:string; defaultTo:string; accounts:Account[]; environments:string[]; teams:string[]; models:string[]; comparisonNote:string; }
export interface Filters { from:string; to:string; provider:string; account:string; environment:string; team:string; model:string; metric:string; granularity:string; }
export interface Summary { totalRequests:number; inputTokens:number|null; outputTokens:number|null; totalTokens:number|null; estimatedCost:number|null; successRate:number; activeProviders:number; connectedAccounts:number; requestChangePercentage:number|null; tokenChangePercentage:number|null; costChangePercentage:number|null; errorRate:number; inputChangePercentage:number|null; outputChangePercentage:number|null; providerChangePercentage:number|null; accountChangePercentage:number|null; successChangePercentage:number|null; }
export interface ProviderSummary {provider:string;displayName:string;color:string;monogram:string;status:string;health:string;requests:number;inputTokens:number|null;outputTokens:number|null;totalTokens:number|null;estimatedCost:number|null;successRate:number;failedRequests:number;connectedAccounts:number;lastSync:string;}
export interface Trend {date:string;provider:string;value:number|null;}
export interface Consumer {name:string;requests:number;tokens:number|null;cost:number|null;}
export interface TopConsumers {accounts:Consumer[];models:Consumer[];teams:Consumer[];environments:Consumer[];}
export interface Usage {id:string;date:string;provider:string;accountId:string;accountName:string;environment:string;team:string;model:string;requestCount:number;inputTokens:number|null;outputTokens:number|null;totalTokens:number|null;cost:number|null;failedRequests:number;successfulRequests:number;successRate:number;}
export interface Page<T>{items:T[];total:number;page:number;pageSize:number;}
export interface Integration {id:string;providerType:string;integrationName:string;account:string;environment:string;team:string;status:string;health:string;enabled:boolean;lastSync:string;lastSuccessfulSync:string|null;createdBy:string;isDefault:boolean;isDemoMode:boolean;hasCredentials:boolean;configuration:Record<string,string>;}
export interface IntegrationRequest {providerType:string;integrationName:string;environment:string;team:string;configuration:Record<string,string>;enabled:boolean;}
export interface ConnectionResult {success:boolean;status:string;message:string;responseTimeMs:number;isDemoMode:boolean;}
export interface SyncResult {integrationId:string;success:boolean;recordsProcessed:number;status:string;message:string;startedAt:string;completedAt:string;}
export interface Audit {id:string;timestamp:string;user:string;action:string;provider:string;integration:string;result:string;correlationId:string;}
export interface Alert {id:string;severity:string;title:string;description:string;provider:string;timestamp:string;}
export interface CostSummary {totalCost:number|null;previousPeriodCost:number|null;percentageChange:number|null;costByProvider:Consumer[];costByAccount:Consumer[];costByModel:Consumer[];costByTeam:Consumer[];}

