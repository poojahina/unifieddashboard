import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
@Component({selector:'ui-confirmation',imports:[MatDialogModule],template:`<h2 mat-dialog-title>{{data.title}}</h2><mat-dialog-content>{{data.message}}</mat-dialog-content><mat-dialog-actions align="end"><button class="btn" [mat-dialog-close]="false">Cancel</button><button class="btn danger" [mat-dialog-close]="true">{{data.action}}</button></mat-dialog-actions>`})
export class ConfirmationDialogComponent {data=inject<{title:string;message:string;action:string}>(MAT_DIALOG_DATA);}

