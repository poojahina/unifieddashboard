import { Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AuditService, DashboardService, IntegrationService, NoticeService } from '../../core/api.services';
import { FilterStore } from '../../core/filter.store';
import { Alert, Audit, Integration, ProviderSummary, TopConsumers } from '../../core/models';
import { FilterBarComponent } from '../../shared/filter-bar.component';
import { StatusComponent, LoadingComponent, ErrorComponent, EmptyComponent } from '../../shared/ui.components';
import { IconComponent } from '../../shared/icon.component';
import { compact, money } from '../../shared/chart.options';
@Component({selector:'app-workspace',imports:[CommonModule,FormsModule,RouterLink,FilterBarComponent,StatusComponent,LoadingComponent,ErrorComponent,EmptyComponent,IconComponent],templateUrl:'./workspace.component.html'})
export class WorkspaceComponent {
 kind=inject(ActivatedRoute).snapshot.data['kind'] as string;
 store=inject(FilterStore);private dashboard=inject(DashboardService);private auditService=inject(AuditService);private integrationService=inject(IntegrationService);notice=inject(NoticeService);
 loading=signal(true);error=signal('');providers=signal<ProviderSummary[]>([]);top=signal<TopConsumers|null>(null);audit=signal<Audit[]>([]);alerts=signal<Alert[]>([]);integrations=signal<Integration[]>([]);search=signal('');page=signal(1);reportType=signal('accounts');
 compact=compact;money=money;Math=Math;
 info:Record<string,{title:string;description:string}>={
 providers:{title:'Providers',description:'Compare consumption and capabilities across your AI portfolio.'},
 accounts:{title:'Accounts',description:'See how consumption is distributed across teams and environments.'},
 reports:{title:'Reports',description:'Create a focused consumption report from the current demo dataset.'},
 alerts:{title:'Alerts',description:'Monitor provider health and consumption signals.'},
 audit:{title:'Audit logs',description:'Trace connection changes, tests, and synchronization activity.'},
 settings:{title:'Workspace settings',description:'Understand your workspace configuration and demo boundaries.'}
 };
 filteredAudit=computed(()=>this.audit().filter(a=>(a.action+' '+a.provider+' '+a.integration+' '+a.result).toLowerCase().includes(this.search().toLowerCase())));
 constructor(){effect(cleanup=>{if(!this.store.ready())return;const f=this.store.filters();this.store.revision();this.loading.set(true);this.error.set('');const sub=forkJoin({providers:this.dashboard.providers(f),top:this.dashboard.consumers(f),audit:this.auditService.list({pageSize:200}),alerts:this.dashboard.alerts(),integrations:this.integrationService.list()}).subscribe({next:r=>{this.providers.set(r.providers);this.top.set(r.top);this.audit.set(r.audit.items);this.alerts.set(r.alerts);this.integrations.set(r.integrations);this.loading.set(false);},error:e=>{this.error.set(e.message);this.loading.set(false);}});cleanup(()=>sub.unsubscribe());});}
 account(name:string){return this.store.metadata()?.accounts.find(a=>a.name===name);}
 export(){
  const type=this.reportType() as keyof TopConsumers;const rows=this.top()?.[type]??[];
  const safe=(v:unknown)=>'"'+String(v??'').replace(/^[=+@-]/,"'$&").replace(/"/g,'""')+'"';
  const csv=[['Name','Requests','Tokens','Estimated Cost USD'],...rows.map(r=>[r.name,r.requests,r.tokens,r.cost])].map(row=>row.map(safe).join(',')).join('\r\n');
  const url=URL.createObjectURL(new Blob(['\uFEFF'+csv],{type:'text/csv;charset=utf-8;'}));const a=document.createElement('a');a.href=url;a.download='unified-ai-'+type+'-'+this.store.filters().from+'.csv';a.click();URL.revokeObjectURL(url);this.notice.show('Report exported for the selected filters.');
 }
}

