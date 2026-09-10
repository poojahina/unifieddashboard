import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { IconComponent } from '../shared/icon.component';
import { ErrorComponent } from '../shared/ui.components';
import { FilterStore } from '../core/filter.store';
import { NoticeService } from '../core/api.services';
@Component({selector:'app-root',imports:[CommonModule,FormsModule,RouterLink,RouterLinkActive,RouterOutlet,IconComponent,ErrorComponent],templateUrl:'./app.component.html'})
export class AppComponent {
 store=inject(FilterStore);notice=inject(NoticeService);collapsed=signal(false);dark=signal(localStorage.getItem('unified-theme')==='dark');dateOpen=signal(false);profileOpen=signal(false);
 links=[{path:'dashboard',label:'Dashboard',icon:'grid'},{path:'usage',label:'Usage Analytics',icon:'chart'},{path:'costs',label:'Cost Analytics',icon:'cost'},{path:'providers',label:'Providers',icon:'providers'},{path:'accounts',label:'Accounts',icon:'accounts'},{path:'integrations',label:'Integrations',icon:'plug'},{path:'reports',label:'Reports',icon:'report'},{path:'alerts',label:'Alerts',icon:'bell'},{path:'audit',label:'Audit Logs',icon:'audit'},{path:'settings',label:'Settings',icon:'settings'}];
 constructor(){document.documentElement.classList.toggle('dark',this.dark());}
 theme(){this.dark.update(v=>!v);document.documentElement.classList.toggle('dark',this.dark());localStorage.setItem('unified-theme',this.dark()?'dark':'light');}
 preset(days:number){const to=this.store.metadata()?.defaultTo;if(!to)return;const date=new Date(to+'T12:00:00Z');date.setUTCDate(date.getUTCDate()-days+1);this.store.update({from:date.toISOString().slice(0,10),to});this.dateOpen.set(false);}
}

