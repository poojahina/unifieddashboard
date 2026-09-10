import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from './icon.component';
@Component({selector:'ui-status',imports:[CommonModule],template:`<span class="status" [class.warning]="value()==='Warning'" [class.critical]="value()==='Failed'||value()==='Critical'" [class.neutral]="value()==='Disabled'||value()==='Info'"><i></i>{{value()}}</span>`})
export class StatusComponent {value=input('Healthy');}
@Component({selector:'ui-kpi',imports:[CommonModule,IconComponent],template:`<article class="kpi"><div class="kpi-label">{{label()}}<ui-icon [name]="icon()"/></div><div class="kpi-value">{{value()}}</div><div class="kpi-comparison"><span [class.negative]="unfavorable()" [class.no-change]="change()===null||change()===0">{{change()===null?'—':(change()!>0?'↗ ':change()!<0?'↘ ':'')+(change()!|number:'1.1-1')+'%'}}</span><span>{{change()===null?'No baseline':'vs previous period'}}</span></div></article>`})
export class KpiComponent {label=input('');value=input('');change=input<number|null>(null);icon=input('activity');unfavorable=input(false);}
@Component({selector:'ui-loading',template:`<div class="loading-state" role="status" aria-label="Loading data"><div class="skeleton h24"></div><div class="skeleton-grid"><div class="skeleton"></div><div class="skeleton"></div><div class="skeleton"></div><div class="skeleton"></div></div><div class="skeleton chart-skeleton"></div></div>`})
export class LoadingComponent {}
@Component({selector:'ui-empty',imports:[IconComponent],template:`<div class="empty-state"><ui-icon name="chart"/><h3>{{title()}}</h3><p>{{message()}}</p><ng-content/></div>`})
export class EmptyComponent {title=input('No usage for this selection');message=input('Try a different date range or reset your filters.');}
@Component({selector:'ui-error',imports:[IconComponent],template:`<div class="error-state" role="alert"><ui-icon name="info"/><div><strong>Unable to load data</strong><p>{{message()}}</p></div><button class="btn" (click)="retry.emit()">Try again</button></div>`})
export class ErrorComponent {message=input('');retry=output<void>();}

