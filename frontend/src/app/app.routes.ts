import { Routes } from '@angular/router';
export const routes:Routes=[
 {path:'',pathMatch:'full',redirectTo:'dashboard'},
 {path:'dashboard',loadComponent:()=>import('./features/dashboard/dashboard.component').then(m=>m.DashboardComponent)},
 {path:'usage',loadComponent:()=>import('./features/analytics/analytics.component').then(m=>m.AnalyticsComponent),data:{kind:'usage'}},
 {path:'costs',loadComponent:()=>import('./features/analytics/analytics.component').then(m=>m.AnalyticsComponent),data:{kind:'costs'}},
 {path:'integrations/add',loadComponent:()=>import('./features/integrations/wizard.component').then(m=>m.WizardComponent)},
 {path:'integrations/:id/edit',loadComponent:()=>import('./features/integrations/wizard.component').then(m=>m.WizardComponent)},
 {path:'integrations',loadComponent:()=>import('./features/integrations/integrations.component').then(m=>m.IntegrationsComponent)},
 ...['providers','accounts','reports','alerts','audit','settings'].map(kind=>({path:kind,loadComponent:()=>import('./features/workspace/workspace.component').then(m=>m.WorkspaceComponent),data:{kind}})),
 {path:'**',redirectTo:'dashboard'}
];
