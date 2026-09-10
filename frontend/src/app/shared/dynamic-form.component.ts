import { Component, input, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ProviderField } from '../core/models';
@Component({selector:'ui-secret-input',imports:[ReactiveFormsModule],template:`<div class="secret-input"><input [id]="fieldId()" [type]="visible()?'text':'password'" [formControl]="control()" autocomplete="new-password" spellcheck="false" [attr.aria-label]="label()"><button type="button" class="text-btn" [attr.aria-label]="visible()?'Hide '+label():'Show '+label()" (click)="visible.set(!visible())">{{visible()?'Hide':'Show'}}</button></div>`})
export class SecretInputComponent {control=input.required<FormControl<string>>();fieldId=input('');label=input('Credential');visible=signal(false);}
@Component({selector:'ui-dynamic-form',imports:[ReactiveFormsModule,SecretInputComponent],template:`
<div class="dynamic-form" [formGroup]="form()">
@for(field of fields();track field.name){@if(visible(field)){
<div class="form-field" [class.full-width]="field.type==='textarea'"><label [for]="'config-'+field.name">{{field.label}} @if(required(field)){<span class="required" aria-hidden="true">*</span>}</label>
@if(field.secret&&field.type!=='textarea'){<ui-secret-input [control]="control(field.name)" [fieldId]="'config-'+field.name" [label]="field.label"/>}
@else if(field.type==='dropdown'){<select [id]="'config-'+field.name" [formControlName]="field.name"><option value="">Select an option</option>@for(option of field.options;track option){<option>{{option}}</option>}</select>}
@else if(field.type==='textarea'){<textarea [id]="'config-'+field.name" [formControlName]="field.name" rows="3" autocomplete="off" spellcheck="false"></textarea>}
@else if(field.type==='boolean'){<select [id]="'config-'+field.name" [formControlName]="field.name"><option value="true">Yes</option><option value="false">No</option></select>}
@else if(field.type==='multiselect'){<select multiple [id]="'config-'+field.name" [formControlName]="field.name">@for(option of field.options;track option){<option>{{option}}</option>}</select>}
@else{<input [id]="'config-'+field.name" [type]="field.type==='url'?'url':field.type==='number'?'number':'text'" [formControlName]="field.name" autocomplete="off">}
@if(field.hint){<small>{{field.hint}}</small>}
@if(control(field.name).invalid&&control(field.name).touched){<small class="field-error">Enter a valid {{field.label.toLowerCase()}}.</small>}
</div>}}
</div>`})
export class DynamicFormComponent {
 fields=input.required<ProviderField[]>();form=input.required<FormGroup<Record<string,FormControl<string>>>>();
 control(name:string){return this.form().controls[name];}
 visible(f:ProviderField){return !f.requiredWhenField||!!f.requiredWhenValues?.includes(this.form().controls[f.requiredWhenField]?.value);}
 required(f:ProviderField){return f.required||!!f.requiredWhenField&&this.visible(f);}
}

