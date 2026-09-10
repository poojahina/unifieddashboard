import { defineConfig } from '@playwright/test';
export default defineConfig({testDir:'./e2e',timeout:90000,workers:1,retries:0,reporter:'list',use:{actionTimeout:12000,baseURL:'http://127.0.0.1:4200',viewport:{width:1440,height:1080},headless:true,channel:'msedge',screenshot:'only-on-failure'},outputDir:'../artifacts/test-results'});
