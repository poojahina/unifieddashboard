import { Component, OnDestroy, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatStepper, MatStepperModule } from '@angular/material/stepper';
import { Subscription } from 'rxjs';
import { IntegrationService, NoticeService, ProviderService } from '../../core/api.services';
import { Provider, ConnectionResult, Integration } from '../../core/models';
import { DynamicFormComponent } from '../../shared/dynamic-form.component';
import { IconComponent } from '../../shared/icon.component';
import { LoadingComponent, ErrorComponent } from '../../shared/ui.components';
@Component({selector:'app-integration-wizard',imports:[CommonModule,RouterLink,ReactiveFormsModule,MatStepperModule,DynamicFormComponent,IconComponent,LoadingComponent,ErrorComponent],templateUrl:'./wizard.component.html'})
export class WizardComponent implements OnDestroy {
 private providerService=inject(ProviderService);private integrations=inject(IntegrationService);private router=inject(Router);private route=inject(ActivatedRoute);private notice=inject(NoticeService);
 stepper=viewChild<MatStepper>('stepper');providers=signal<Provider[]>([]);schema=signal<Provider|null>(null);loading=signal(true);busy=signal(false);error=signal('');result=signal<ConnectionResult|null>(null);saved=signal<Integration|null>(null);
 id=this.route.snapshot.paramMap.get('id')??undefined;existing?:Integration;private changes?:Subscription;
 form=new FormGroup({integrationName:new FormControl('',{nonNullable:true,validators:[Validators.required,Validators.maxLength(120)]}),environment:new FormControl('Production',{nonNullable:true,validators:[Validators.required]}),team:new FormControl('Engineering',{nonNullable:true,validators:[Validators.required,Validators.maxLength(120)]})});
 config=new FormGroup<Record<string,FormControl<string>>>({});
 constructor(){this.load();}
 load(){this.loading.set(true);this.error.set('');this.providerService.list().subscribe({next:providers=>{this.providers.set(providers);if(this.id){this.integrations.get(this.id).subscribe({next:i=>{this.existing=i;this.form.patchValue(i);this.choose(i.providerType);},error:e=>{this.error.set(e.message);this.loading.set(false);}});}else this.loading.set(false);},error:e=>{this.error.set(e.message);this.loading.set(false);}});}
 choose(type:string){
  this.busy.set(true);this.error.set('');this.result.set(null);this.changes?.unsubscribe();
  this.providerService.schema(type).subscribe({next:schema=>{
   this.schema.set(schema);const controls:Record<string,FormControl<string>>={};
   for(const f of schema.fields)controls[f.name]=new FormControl(this.existing?.configuration[f.name]??'',{nonNullable:true});
   this.config=new FormGroup(controls);this.validateFields();
   this.changes=this.config.valueChanges.subscribe(()=>{this.validateFields();this.result.set(null);});
   this.busy.set(false);this.loading.set(false);
  },error:e=>{this.error.set(e.message);this.busy.set(false);this.loading.set(false);}});
 }
 validateFields(){
  for(const f of this.schema()?.fields??[]){
   const required=f.required||!!f.requiredWhenField&&!!f.requiredWhenValues?.includes(this.config.controls[f.requiredWhenField]?.value);
   const validators=[Validators.maxLength(16000)];if(required)validators.push(Validators.required);
   if(f.type==='url')validators.push(Validators.pattern(/^https:\/\/[^\s]+$/));
   this.config.controls[f.name].setValidators(validators);this.config.controls[f.name].updateValueAndValidity({emitEvent:false});
  }
 }
 nextDetails(){this.form.markAllAsTouched();this.config.markAllAsTouched();if(this.form.valid&&this.config.valid)this.stepper()?.next();}
 request(){return {providerType:this.schema()!.providerType,...this.form.getRawValue(),configuration:this.config.getRawValue(),enabled:this.existing?.enabled??true};}
 test(){
  this.busy.set(true);this.error.set('');this.result.set(null);
  this.integrations.test(this.request()).subscribe({next:r=>{this.result.set(r);this.busy.set(false);},error:e=>{this.error.set(e.message);this.busy.set(false);}});
 }
 save(){
  if(!this.result()?.success)return;this.busy.set(true);this.error.set('');
  this.integrations.save(this.request(),this.id).subscribe({next:i=>{this.saved.set(i);this.config.reset();this.busy.set(false);this.notice.show('Integration '+(this.id?'updated':'added')+' for this runtime.');},error:e=>{this.error.set(e.message);this.busy.set(false);}});
 }
 sync(){const i=this.saved();if(!i)return;this.busy.set(true);this.integrations.sync(i.id).subscribe({next:r=>{this.notice.show(r.message);void this.router.navigate(['/integrations']);},error:e=>{this.error.set(e.message);this.busy.set(false);}});}
 ngOnDestroy(){this.changes?.unsubscribe();this.config.reset();}
}

