import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpInterceptorFn, HttpParams } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { Alert, Audit, ConnectionResult, CostSummary, Filters, Integration, IntegrationRequest, Metadata, Page, Provider, ProviderSummary, Summary, SyncResult, TopConsumers, Trend, Usage } from './models';

export const errorInterceptor:HttpInterceptorFn=(req,next)=>next(req).pipe(catchError((e:HttpErrorResponse)=>{
  const fallback:Record<number,string>={0:'Cannot reach the API. Check that the backend is running on port 5080.',400:'Check the supplied values.',401:'Authentication is required.',403:'This action is not permitted.',404:'The requested item was not found.',408:'The request timed out.',409:'This action conflicts with the current state.',429:'Too many requests. Please wait a moment.',500:'The service could not complete this request.',502:'The upstream service is unavailable.',503:'The service is temporarily unavailable.'};
  return throwError(()=>new Error(typeof e.error?.title==='string'?e.error.title:fallback[e.status]??'The request failed. Please retry.'));
}));
@Injectable({providedIn:'root'})
export class ApiClient {
  private http=inject(HttpClient);
  get<T>(path:string,params:object={}) { let query=new HttpParams(); for(const [k,v] of Object.entries(params))if(v!==''&&v!==null&&v!==undefined)query=query.set(k,String(v));return this.http.get<T>(environment.apiUrl+path,{params:query});}
  post<T>(path:string,body:unknown={}){return this.http.post<T>(environment.apiUrl+path,body);}
  put<T>(path:string,body:unknown){return this.http.put<T>(environment.apiUrl+path,body);}
  patch<T>(path:string,body:unknown){return this.http.patch<T>(environment.apiUrl+path,body);}
  delete(path:string){return this.http.delete<void>(environment.apiUrl+path);}
}
@Injectable({providedIn:'root'}) export class DashboardService {
  private api=inject(ApiClient);
  metadata(){return this.api.get<Metadata>('/metadata');}
  summary(f:Filters){return this.api.get<Summary>('/dashboard/summary',f);}
  providers(f:Filters){return this.api.get<ProviderSummary[]>('/dashboard/provider-summary',f);}
  consumers(f:Filters){return this.api.get<TopConsumers>('/dashboard/top-consumers',f);}
  alerts(){return this.api.get<Alert[]>('/alerts');}
}
@Injectable({providedIn:'root'}) export class UsageService {
  private api=inject(ApiClient);
  trends(f:Filters){return this.api.get<Trend[]>('/usage/trends',f);}
  list(f:Filters,page=1,sortBy='date',sortDirection='desc'){return this.api.get<Page<Usage>>('/usage',{...f,page,pageSize:15,sortBy,sortDirection});}
}
@Injectable({providedIn:'root'}) export class CostService {
  private api=inject(ApiClient);
  summary(f:Filters){return this.api.get<CostSummary>('/costs/summary',f);}
  trends(f:Filters){return this.api.get<Trend[]>('/costs/trends',f);}
}
@Injectable({providedIn:'root'}) export class ProviderService {
  private api=inject(ApiClient);
  list(){return this.api.get<Provider[]>('/providers');}
  schema(type:string){return this.api.get<Provider>('/providers/'+encodeURIComponent(type)+'/configuration-schema');}
}
@Injectable({providedIn:'root'}) export class IntegrationService {
  private api=inject(ApiClient);
  list(){return this.api.get<Integration[]>('/integrations');}
  get(id:string){return this.api.get<Integration>('/integrations/'+id);}
  save(r:IntegrationRequest,id?:string){return id?this.api.put<Integration>('/integrations/'+id,r):this.api.post<Integration>('/integrations',r);}
  test(r:IntegrationRequest){return this.api.post<ConnectionResult>('/integrations/test',r);}
  testExisting(id:string){return this.api.post<ConnectionResult>('/integrations/'+id+'/test');}
  sync(id:string){return this.api.post<SyncResult>('/integrations/'+id+'/sync');}
  history(id:string){return this.api.get<SyncResult[]>('/integrations/'+id+'/sync-history');}
  toggle(id:string,enabled:boolean){return this.api.patch<Integration>('/integrations/'+id+'/enabled',{enabled});}
  delete(id:string){return this.api.delete('/integrations/'+id);}
}
@Injectable({providedIn:'root'}) export class AuditService {
  private api=inject(ApiClient);
  list(params:object={}){return this.api.get<Page<Audit>>('/audit-logs',params);}
}
@Injectable({providedIn:'root'}) export class NoticeService {
  message=signal('');private timer:ReturnType<typeof setTimeout>|undefined;
  show(message:string){this.message.set(message);clearTimeout(this.timer);this.timer=setTimeout(()=>this.message.set(''),6500);}
}

