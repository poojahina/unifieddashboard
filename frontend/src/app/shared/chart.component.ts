import { AfterViewInit, Component, ElementRef, OnDestroy, effect, inject, input, viewChild } from '@angular/core';
import * as echarts from 'echarts/core';
import { LineChart, BarChart, PieChart } from 'echarts/charts';
import { GridComponent, TooltipComponent, LegendComponent, TitleComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import { EChartsCoreOption, EChartsType } from 'echarts/core';
echarts.use([LineChart,BarChart,PieChart,GridComponent,TooltipComponent,LegendComponent,TitleComponent,CanvasRenderer]);
@Component({selector:'ui-chart',template:`<div #canvas class="chart-canvas" role="img" [attr.aria-label]="label()" [style.height.px]="height()"></div>`})
export class ChartComponent implements AfterViewInit,OnDestroy {
  options=input.required<EChartsCoreOption>();label=input('Interactive analytics chart');height=input(280);
  canvas=viewChild.required<ElementRef<HTMLDivElement>>('canvas');private chart?:EChartsType;private resize?:ResizeObserver;
  constructor(){effect(()=>{const options=this.options();this.chart?.setOption(options,true);});}
  ngAfterViewInit(){this.chart=echarts.init(this.canvas().nativeElement);this.chart.setOption(this.options());this.resize=new ResizeObserver(()=>this.chart?.resize());this.resize.observe(this.canvas().nativeElement);}
  ngOnDestroy(){this.resize?.disconnect();this.chart?.dispose();}
}

