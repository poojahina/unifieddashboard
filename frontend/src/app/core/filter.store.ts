import { Injectable, inject, signal } from '@angular/core';
import { DashboardService, ProviderService } from './api.services';
import { Filters, Metadata, Provider } from './models';
import { forkJoin } from 'rxjs';
@Injectable({providedIn:'root'})
export class FilterStore {
  private dashboard=inject(DashboardService);private providers=inject(ProviderService);
  metadata=signal<Metadata|null>(null);catalog=signal<Provider[]>([]);error=signal('');ready=signal(false);revision=signal(0);
  filters=signal<Filters>({from:'',to:'',provider:'',account:'',environment:'',team:'',model:'',metric:'requests',granularity:'daily'});
  constructor(){this.load();}
  load(){this.error.set('');forkJoin({metadata:this.dashboard.metadata(),providers:this.providers.list()}).subscribe({next:r=>{this.metadata.set(r.metadata);this.catalog.set(r.providers);this.filters.update(f=>({...f,from:r.metadata.defaultFrom,to:r.metadata.defaultTo}));this.ready.set(true);},error:e=>this.error.set(e.message)});}
  update(patch:Partial<Filters>){this.filters.update(f=>({...f,...patch}));}
  reset(){const m=this.metadata();if(m)this.filters.set({from:m.defaultFrom,to:m.defaultTo,provider:'',account:'',environment:'',team:'',model:'',metric:'requests',granularity:'daily'});}
  refresh(){this.revision.update(x=>x+1);}
}

