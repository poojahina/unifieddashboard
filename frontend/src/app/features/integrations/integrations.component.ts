import { Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { A11yModule } from '@angular/cdk/a11y';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialog } from '@angular/material/dialog';
import { IntegrationService, NoticeService } from '../../core/api.services';
import { FilterStore } from '../../core/filter.store';
import { Integration, SyncResult } from '../../core/models';
import { IconComponent } from '../../shared/icon.component';
import { StatusComponent, LoadingComponent, ErrorComponent, EmptyComponent } from '../../shared/ui.components';
import { ConfirmationDialogComponent } from '../../shared/confirmation-dialog.component';
@Component({selector:'app-integrations',imports:[CommonModule,FormsModule,RouterLink,MatMenuModule,A11yModule,IconComponent,StatusComponent,LoadingComponent,ErrorComponent,EmptyComponent],templateUrl:'./integrations.component.html'})
export class IntegrationsComponent {
 private api=inject(IntegrationService);private dialog=inject(MatDialog);notice=inject(NoticeService);store=inject(FilterStore);
 rows=signal<Integration[]>([]);loading=signal(true);error=signal('');search=signal('');busy=signal('');selected=signal<Integration|null>(null);history=signal<SyncResult[]>([]);
 visible=computed(()=>this.rows().filter(i=>(i.integrationName+' '+i.providerType+' '+i.environment).toLowerCase().includes(this.search().toLowerCase())));
 healthy=computed(()=>this.rows().filter(i=>i.enabled&&i.health==='Healthy').length);
 constructor(){effect(()=>{this.store.revision();this.load();});}
 load(){this.loading.set(true);this.error.set('');this.api.list().subscribe({next:r=>{this.rows.set(r);this.loading.set(false);},error:e=>{this.error.set(e.message);this.loading.set(false);}});}
 test(i:Integration){this.busy.set(i.id);this.api.testExisting(i.id).subscribe({next:r=>{this.notice.show(r.status+' · '+r.message);this.busy.set('');},error:e=>this.fail(e)});}
 sync(i:Integration){this.busy.set(i.id);this.api.sync(i.id).subscribe({next:r=>{this.notice.show(r.message);this.busy.set('');this.load();},error:e=>this.fail(e)});}
 toggle(i:Integration){this.busy.set(i.id);this.api.toggle(i.id,!i.enabled).subscribe({next:()=>{this.busy.set('');this.load();},error:e=>this.fail(e)});}
 remove(i:Integration){this.dialog.open(ConfirmationDialogComponent,{width:'430px',data:{title:'Delete integration?',message:'Remove '+i.integrationName+' from this runtime? Historical demo consumption will remain unchanged.',action:'Delete integration'}}).afterClosed().subscribe(ok=>{if(ok){this.busy.set(i.id);this.api.delete(i.id).subscribe({next:()=>{this.busy.set('');this.notice.show('Integration removed from this runtime.');this.load();},error:e=>this.fail(e)});}});}
 view(i:Integration){this.selected.set(i);this.history.set([]);this.api.history(i.id).subscribe({next:r=>this.history.set(r),error:e=>this.notice.show(e.message)});}
 private fail(e:Error){this.busy.set('');this.notice.show(e.message);}
}

