import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FilterStore } from '../core/filter.store';
import { IconComponent } from './icon.component';
@Component({selector:'ui-filter-bar',imports:[FormsModule,IconComponent],template:`
<div class="filter-bar">
 <span class="filter-label"><ui-icon name="settings"/>Filters</span>
 <select aria-label="Provider filter" [ngModel]="store.filters().provider" (ngModelChange)="provider($event)"><option value="">All providers</option>@for(p of store.catalog();track p.providerType){<option [value]="p.providerType">{{p.displayName}}</option>}</select>
 <select aria-label="Account filter" [ngModel]="store.filters().account" (ngModelChange)="store.update({account:$event})"><option value="">All accounts</option>@for(a of accounts();track a.id){<option [value]="a.id">{{a.name}}</option>}</select>
 <select aria-label="Team filter" [ngModel]="store.filters().team" (ngModelChange)="store.update({team:$event})"><option value="">All teams</option>@for(t of store.metadata()?.teams;track t){<option>{{t}}</option>}</select>
 <select aria-label="Model filter" [ngModel]="store.filters().model" (ngModelChange)="store.update({model:$event})"><option value="">All models</option>@for(m of models();track m){<option>{{m}}</option>}</select>
 <span class="filter-spacer"></span><button class="text-btn" (click)="store.reset()">Reset filters</button>
</div>`})
export class FilterBarComponent {
 store=inject(FilterStore);
 provider(value:string){this.store.update({provider:value,account:'',model:''});}
 accounts(){return this.store.metadata()?.accounts.filter(a=>!this.store.filters().provider||a.provider===this.store.filters().provider)??[];}
 models(){return [...new Set(this.accounts().map(a=>a.model))];}
}

