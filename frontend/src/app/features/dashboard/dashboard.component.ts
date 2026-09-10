import { Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { DashboardService, UsageService, CostService, AuditService } from '../../core/api.services';
import { FilterStore } from '../../core/filter.store';
import { Audit, ProviderSummary, Summary, TopConsumers, Trend } from '../../core/models';
import { FilterBarComponent } from '../../shared/filter-bar.component';
import { IconComponent } from '../../shared/icon.component';
import { KpiComponent, StatusComponent, LoadingComponent, EmptyComponent, ErrorComponent } from '../../shared/ui.components';
import { ChartComponent } from '../../shared/chart.component';
import { compact, money, lineOptions, donutOptions, tokenOptions } from '../../shared/chart.options';
@Component({selector:'app-dashboard',imports:[CommonModule,FormsModule,RouterLink,FilterBarComponent,IconComponent,KpiComponent,StatusComponent,LoadingComponent,EmptyComponent,ErrorComponent,ChartComponent],templateUrl:'./dashboard.component.html'})
export class DashboardComponent {
 store=inject(FilterStore);private dashboard=inject(DashboardService);private usage=inject(UsageService);private costs=inject(CostService);private audit=inject(AuditService);
 loading=signal(true);error=signal('');summary=signal<Summary|null>(null);providers=signal<ProviderSummary[]>([]);trends=signal<Trend[]>([]);costTrends=signal<Trend[]>([]);top=signal<TopConsumers|null>(null);activity=signal<Audit[]>([]);consumerTab=signal<'accounts'|'models'|'teams'>('accounts');
 compact=compact;money=money;
 usageChart=computed(()=>lineOptions(this.trends(),this.providers(),this.store.filters().metric));
 costChart=computed(()=>lineOptions(this.costTrends(),this.providers(),'cost'));
 donut=computed(()=>donutOptions(this.providers()));tokens=computed(()=>tokenOptions(this.providers()));
 total=computed(()=>this.providers().reduce((sum,p)=>sum+p.requests,0));
 constructor(){effect(onCleanup=>{if(!this.store.ready())return;const filters=this.store.filters();this.store.revision();this.loading.set(true);this.error.set('');const sub=forkJoin({summary:this.dashboard.summary(filters),providers:this.dashboard.providers(filters),trends:this.usage.trends(filters),cost:this.costs.trends(filters),top:this.dashboard.consumers(filters),audit:this.audit.list({pageSize:4})}).subscribe({next:r=>{this.summary.set(r.summary);this.providers.set(r.providers);this.trends.set(r.trends);this.costTrends.set(r.cost);this.top.set(r.top);this.activity.set(r.audit.items);this.loading.set(false);},error:e=>{this.error.set(e.message);this.loading.set(false);}});onCleanup(()=>sub.unsubscribe());});}
}

