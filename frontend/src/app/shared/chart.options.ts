import { EChartsCoreOption } from 'echarts/core';
import { ProviderSummary, Trend } from '../core/models';
const axis={axisLine:{show:false},axisTick:{show:false},axisLabel:{color:'#86909d',fontSize:10},splitLine:{lineStyle:{color:'rgba(140,150,165,.14)',type:'dashed'}}};
export const compact=(n:number|null,digits=1)=>n===null?'—':new Intl.NumberFormat('en-US',{notation:'compact',maximumFractionDigits:digits}).format(n);
export const money=(n:number|null)=>n===null?'—':new Intl.NumberFormat('en-US',{style:'currency',currency:'USD',maximumFractionDigits:2}).format(n);
export function lineOptions(points:Trend[],providers:ProviderSummary[],metric:string):EChartsCoreOption {
 const dates=[...new Set(points.map(x=>x.date))].sort();
 return {animationDuration:350,color:providers.map(p=>p.color),textStyle:{fontFamily:'Inter, Segoe UI, sans-serif'},tooltip:{trigger:'axis',valueFormatter:(value:unknown)=>metric==='cost'?money(Number(value)):new Intl.NumberFormat('en-US').format(Number(value))},grid:{left:50,right:20,top:25,bottom:32},xAxis:{...axis,type:'category',data:dates,axisLabel:{...axis.axisLabel,formatter:(s:string)=>new Date(s+'T00:00:00').toLocaleDateString('en-US',{month:'short',day:'numeric'})},boundaryGap:false},yAxis:{...axis,type:'value',axisLabel:{...axis.axisLabel,formatter:(n:number)=>metric==='cost'?'$'+compact(n):compact(n)}},series:providers.map(p=>({name:p.displayName,type:'line',smooth:.25,showSymbol:false,symbolSize:6,lineStyle:{width:2.5},areaStyle:{opacity:.035},data:dates.map(d=>points.find(x=>x.date===d&&x.provider===p.provider)?.value??null)}))};
}
export function donutOptions(providers:ProviderSummary[]):EChartsCoreOption {
 return {animationDuration:350,color:providers.map(x=>x.color),tooltip:{trigger:'item',formatter:'{b}<br/>{c} requests · {d}%'},series:[{type:'pie',radius:['67%','85%'],center:['50%','50%'],label:{show:false},itemStyle:{borderColor:'transparent',borderWidth:3,borderRadius:3},data:providers.map(p=>({name:p.displayName,value:p.requests}))}]};
}
export function tokenOptions(providers:ProviderSummary[]):EChartsCoreOption {
 return {color:['#5f7b97','#bccbda'],tooltip:{trigger:'axis',axisPointer:{type:'shadow'}},legend:{bottom:0,icon:'roundRect',itemWidth:10,itemHeight:10,textStyle:{color:'#84909c',fontSize:11}},grid:{left:52,right:16,top:18,bottom:55},xAxis:{...axis,type:'category',data:providers.map(p=>p.provider)},yAxis:{...axis,type:'value',axisLabel:{...axis.axisLabel,formatter:(n:number)=>compact(n)}},series:[{name:'Input tokens',type:'bar',stack:'tokens',barMaxWidth:32,data:providers.map(p=>p.inputTokens)},{name:'Output tokens',type:'bar',stack:'tokens',barMaxWidth:32,itemStyle:{borderRadius:[3,3,0,0]},data:providers.map(p=>p.outputTokens)}]};
}

export function outcomeOptions(providers:ProviderSummary[]):EChartsCoreOption {
 return {color:['#7ba894','#c78b7d'],tooltip:{trigger:'axis',axisPointer:{type:'shadow'}},legend:{bottom:0,icon:'roundRect',itemWidth:10,itemHeight:10,textStyle:{color:'#84909c',fontSize:11}},grid:{left:90,right:25,top:12,bottom:48},xAxis:{...axis,type:'value',axisLabel:{...axis.axisLabel,formatter:(n:number)=>compact(n)}},yAxis:{...axis,type:'category',data:providers.map(p=>p.provider),axisLabel:{color:'#84909c',fontSize:11}},series:[{name:'Successful requests',type:'bar',stack:'outcomes',barMaxWidth:24,data:providers.map(p=>p.requests-p.failedRequests)},{name:'Failed requests',type:'bar',stack:'outcomes',barMaxWidth:24,itemStyle:{borderRadius:[0,3,3,0]},data:providers.map(p=>p.failedRequests)}]};
}
