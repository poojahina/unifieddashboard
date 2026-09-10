import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { DashboardService, IntegrationService, errorInterceptor } from './api.services';
import { Filters } from './models';
describe('API services',()=>{
 let http:HttpTestingController;
 beforeEach(()=>{TestBed.configureTestingModule({providers:[provideHttpClient(withInterceptors([errorInterceptor])),provideHttpClientTesting()]});http=TestBed.inject(HttpTestingController);});
 afterEach(()=>http.verify());
 it('sends global filters to the dashboard API',()=>{
  const filters:Filters={from:'2026-08-12',to:'2026-09-10',provider:'Claude',account:'',environment:'Production',team:'',model:'',metric:'requests',granularity:'daily'};
  TestBed.inject(DashboardService).summary(filters).subscribe();
  const req=http.expectOne(r=>r.url==='/api/dashboard/summary');expect(req.request.params.get('provider')).toBe('Claude');expect(req.request.params.has('account')).toBe(false);req.flush({});
 });
 it('sends entered credentials only in the request body',()=>{
  TestBed.inject(IntegrationService).test({providerType:'Claude',integrationName:'Demo',environment:'Production',team:'Engineering',enabled:true,configuration:{adminApiKey:'dummy-key'}}).subscribe();
  const req=http.expectOne('/api/integrations/test');expect(req.request.method).toBe('POST');expect(req.request.body.configuration.adminApiKey).toBe('dummy-key');expect(req.request.urlWithParams).not.toContain('dummy-key');req.flush({success:true});
 });
 it('turns network errors into a useful message',()=>{
  let message='';TestBed.inject(IntegrationService).list().subscribe({error:e=>message=e.message});
  http.expectOne('/api/integrations').error(new ProgressEvent('error'));expect(message).toContain('backend is running');
 });
});

