import { Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { DashboardService, UsageService, CostService } from '../../core/api.services';
import { FilterStore } from '../../core/filter.store';
import { CostSummary, Page, ProviderSummary, Summary, Trend, Usage } from '../../core/models';
import { FilterBarComponent } from '../../shared/filter-bar.component';
import { ChartComponent } from '../../shared/chart.component';
import { KpiComponent, LoadingComponent, EmptyComponent, ErrorComponent } from '../../shared/ui.components';
import { lineOptions, outcomeOptions, compact, money } from '../../shared/chart.options';
@Component({selector:'app-analytics',imports:[CommonModule,FormsModule,FilterBarComponent,ChartComponent,KpiComponent,LoadingComponent,EmptyComponent,ErrorComponent],templateUrl:'./analytics.component.html'})
export class AnalyticsComponent {
 kind=inject(ActivatedRoute).snapshot.data['kind'] as string;store=inject(FilterStore);private usage=inject(UsageService);private dashboard=inject(DashboardService);private costs=inject(CostService);
 loading=signal(true);error=signal('');page=signal(1);sort=signal('date');direction=signal('desc');rows=signal<Page<Usage>|null>(null);summary=signal<Summary|null>(null);cost=signal<CostSummary|null>(null);trends=signal<Trend[]>([]);providers=signal<ProviderSummary[]>([]);
 outcomes=computed(()=>outcomeOptions(this.providers()));
 chart=computed(()=>lineOptions(this.trends(),this.providers(),this.kind==='costs'?'cost':this.store.filters().metric));compact=compact;money=money;Math=Math;
 constructor(){
  effect(()=>{this.store.filters();this.page.set(1);});
  effect(cleanup=>{if(!this.store.ready())return;const f=this.store.filters();this.store.revision();const page=this.page(),sort=this.sort(),direction=this.direction();this.loading.set(true);this.error.set('');const sub=forkJoin({rows:this.usage.list(f,page,sort,direction),summary:this.dashboard.summary(f),cost:this.costs.summary(f),trends:this.kind==='costs'?this.costs.trends(f):this.usage.trends(f),providers:this.dashboard.providers(f)}).subscribe({next:r=>{this.rows.set(r.rows);this.summary.set(r.summary);this.cost.set(r.cost);this.trends.set(r.trends);this.providers.set(r.providers);this.loading.set(false);},error:e=>{this.error.set(e.message);this.loading.set(false);}});cleanup(()=>sub.unsubscribe());});
 }
 order(key:string){if(this.sort()===key)this.direction.update(x=>x==='desc'?'asc':'desc');else{this.sort.set(key);this.direction.set('desc');}this.page.set(1);}
}

