import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { AppComponent } from './app/layout/app.component';
import { routes } from './app/app.routes';
import { errorInterceptor } from './app/core/api.services';
bootstrapApplication(AppComponent,{providers:[provideHttpClient(withInterceptors([errorInterceptor])),provideRouter(routes,withComponentInputBinding())]}).catch(console.error);
