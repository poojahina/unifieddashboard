import { Component, input } from '@angular/core';
@Component({selector:'ui-icon',standalone:true,template:`<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.65" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path [attr.d]="paths[name()] || paths['grid']"/></svg>`,styles:[`:host{display:inline-flex;width:19px;height:19px;flex-shrink:0}svg{width:100%;height:100%}`]})
export class IconComponent {
 name=input('grid');
 paths:Record<string,string>={
 grid:'M3 3h7v7H3z M14 3h7v7h-7z M3 14h7v7H3z M14 14h7v7h-7z',
 chart:'M4 3v17h17 M8 15l4-5 4 2 5-7',
 cost:'M12 2v20 M17 6H9a3 3 0 000 6h6a3 3 0 010 6H6',
 providers:'M12 3l9 5-9 5-9-5z M3 12l9 5 9-5 M3 16l9 5 9-5',
 accounts:'M16 21v-2a4 4 0 00-4-4H6a4 4 0 00-4 4v2 M9 11a4 4 0 100-8 4 4 0 000 8 M20 21v-2a4 4 0 00-3-3.87 M16 3.13a4 4 0 010 7.75',
 plug:'M8 3v5 M16 3v5 M5 8h14 M7 8v5a5 5 0 0010 0V8 M12 18v4',
 report:'M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z M14 2v6h6 M8 12h8 M8 16h6',
 bell:'M18 8a6 6 0 00-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9 M10 21h4',
 audit:'M9 3H5v18h14V3h-4 M9 2h6v4H9z M8 10h8 M8 14h8 M8 18h5',
 settings:'M12 8a4 4 0 100 8 4 4 0 000-8 M9 3h6l1 3 3 1 2 5-2 5-3 1-1 3H9l-1-3-3-1-2-5 2-5 3-1z',
 chevron:'M9 5l7 7-7 7',down:'M6 9l6 6 6-6',refresh:'M20 7v5h-5 M4 17v-5h5 M6 7a7 7 0 0112-2l2 3 M4 16l2 3a7 7 0 0012-2',
 plus:'M12 5v14 M5 12h14',download:'M12 3v12 M7 10l5 5 5-5 M4 16v5h16v-5',
 sun:'M12 8a4 4 0 100 8 4 4 0 000-8 M12 2v2 M12 20v2 M2 12h2 M20 12h2 M5 5l1 1 M18 18l1 1 M5 19l1-1 M18 6l1-1',
 moon:'M20 15A9 9 0 019 3a9 9 0 1011 12',search:'M10 3a7 7 0 100 14 7 7 0 000-14 M15 15l6 6',
 calendar:'M5 4h14a2 2 0 012 2v14H3V6a2 2 0 012-2z M7 2v4 M17 2v4 M3 10h18',
 arrow:'M5 12h14 M14 7l5 5-5 5',check:'M5 12l4 4L19 6',close:'M6 6l12 12 M6 18L18 6',
 info:'M12 3a9 9 0 100 18 9 9 0 000-18 M12 11v6 M12 7h.01',
 activity:'M2 12h5l3-8 4 16 3-8h5',shield:'M12 2l9 4v6c0 5-9 10-9 10S3 17 3 12V6z M8 12l3 3 5-6',
 collapse:'M3 3h18v18H3z M8 3v18 M15 8l-4 4 4 4',
 clock:'M12 3a9 9 0 100 18 9 9 0 000-18 M12 7v5l3 2',
 more:'M5 12h.01 M12 12h.01 M19 12h.01'
 };
}

