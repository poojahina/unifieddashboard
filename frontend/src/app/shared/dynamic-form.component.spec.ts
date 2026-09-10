import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { DynamicFormComponent } from './dynamic-form.component';
describe('Dynamic credential form',()=>{
 it('renders a password field from API metadata',async()=>{
  await TestBed.configureTestingModule({imports:[DynamicFormComponent]}).compileComponents();
  const fixture=TestBed.createComponent(DynamicFormComponent);
  fixture.componentRef.setInput('fields',[{name:'adminApiKey',label:'Admin API key',type:'password',required:true,secret:true}]);
  fixture.componentRef.setInput('form',new FormGroup({adminApiKey:new FormControl('',{nonNullable:true})}));
  fixture.detectChanges();expect(fixture.nativeElement.querySelector('input').type).toBe('password');
  expect(fixture.nativeElement.textContent).toContain('Admin API key');
 });
 it('hides fields for a different authentication type',async()=>{
  await TestBed.configureTestingModule({imports:[DynamicFormComponent]}).compileComponents();
  const fixture=TestBed.createComponent(DynamicFormComponent);
  fixture.componentRef.setInput('fields',[{name:'password',label:'Password',type:'password',secret:true,requiredWhenField:'authenticationType',requiredWhenValues:['Basic']}]);
  fixture.componentRef.setInput('form',new FormGroup({password:new FormControl('',{nonNullable:true}),authenticationType:new FormControl('OAuth2',{nonNullable:true})}));
  fixture.detectChanges();expect(fixture.nativeElement.querySelector('input')).toBeNull();
 });
});

