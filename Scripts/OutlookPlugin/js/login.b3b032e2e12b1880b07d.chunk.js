(window.webpackJsonp=window.webpackJsonp||[]).push([[3],{"9Xmv":function(n,t,e){"use strict";e.r(t),e.d(t,"Login",(function(){return s}));var r=e("aurelia-framework"),o=e("nuc8"),i=e("MiHq"),l=e("hGdZ"),__decorate=function(n,t,e,r){var o,i=arguments.length,l=i<3?t:null===r?r=Object.getOwnPropertyDescriptor(t,e):r;if("object"==typeof Reflect&&"function"==typeof Reflect.decorate)l=Reflect.decorate(n,t,e,r);else for(var a=n.length-1;a>=0;a--)(o=n[a])&&(l=(i<3?o(l):i>3?o(t,e,l):o(t,e))||l);return i>3&&l&&Object.defineProperty(t,e,l),l},__metadata=function(n,t){if("object"==typeof Reflect&&"function"==typeof Reflect.metadata)return Reflect.metadata(n,t)},__awaiter=function(n,t,e,r){return new(e||(e=Promise))((function(o,i){function fulfilled(n){try{step(r.next(n))}catch(n){i(n)}}function rejected(n){try{step(r.throw(n))}catch(n){i(n)}}function step(n){n.done?o(n.value):function adopt(n){return n instanceof e?n:new e((function(t){t(n)}))}(n.value).then(fulfilled,rejected)}step((r=r.apply(n,t||[])).next())}))},__generator=function(n,t){var e,r,o,i,l={label:0,sent:function(){if(1&o[0])throw o[1];return o[1]},trys:[],ops:[]};return i={next:verb(0),throw:verb(1),return:verb(2)},"function"==typeof Symbol&&(i[Symbol.iterator]=function(){return this}),i;function verb(i){return function(a){return function step(i){if(e)throw new TypeError("Generator is already executing.");for(;l;)try{if(e=1,r&&(o=2&i[0]?r.return:i[0]?r.throw||((o=r.return)&&o.call(r),0):r.next)&&!(o=o.call(r,i[1])).done)return o;switch(r=0,o&&(i=[2&i[0],o.value]),i[0]){case 0:case 1:o=i;break;case 4:return l.label++,{value:i[1],done:!1};case 5:l.label++,r=i[1],i=[0];continue;case 7:i=l.ops.pop(),l.trys.pop();continue;default:if(!(o=l.trys,(o=o.length>0&&o[o.length-1])||6!==i[0]&&2!==i[0])){l=0;continue}if(3===i[0]&&(!o||i[1]>o[0]&&i[1]<o[3])){l.label=i[1];break}if(6===i[0]&&l.label<o[1]){l.label=o[1],o=i;break}if(o&&l.label<o[2]){l.label=o[2],l.ops.push(i);break}o[2]&&l.ops.pop(),l.trys.pop();continue}i=t.call(n,l)}catch(n){i=[6,n],r=0}finally{e=o=0}if(5&i[0])throw i[1];return{value:i[0]?i[1]:void 0,done:!0}}([i,a])}}},a=function(){function LoginApiClient(n){this.client=n}return LoginApiClient.prototype.getCompanies=function(){return __awaiter(this,void 0,void 0,(function(){return __generator(this,(function(n){return[2,this.client.fetch("newui/ts/companies").then((function(n){return n.json()}))]}))}))},LoginApiClient.prototype.getLocalesFor=function(n){return __awaiter(this,void 0,void 0,(function(){return __generator(this,(function(t){return[2,this.client.fetch("newui/ts/localesFor/"+n).then((function(n){return n.json()}))]}))}))},LoginApiClient=__decorate([r.m,__metadata("design:paramtypes",[l.a])],LoginApiClient)}(),c=e("ukFb"),login_decorate=function(n,t,e,r){var o,i=arguments.length,l=i<3?t:null===r?r=Object.getOwnPropertyDescriptor(t,e):r;if("object"==typeof Reflect&&"function"==typeof Reflect.decorate)l=Reflect.decorate(n,t,e,r);else for(var a=n.length-1;a>=0;a--)(o=n[a])&&(l=(i<3?o(l):i>3?o(t,e,l):o(t,e))||l);return i>3&&l&&Object.defineProperty(t,e,l),l},login_metadata=function(n,t){if("object"==typeof Reflect&&"function"==typeof Reflect.metadata)return Reflect.metadata(n,t)},login_awaiter=function(n,t,e,r){return new(e||(e=Promise))((function(o,i){function fulfilled(n){try{step(r.next(n))}catch(n){i(n)}}function rejected(n){try{step(r.throw(n))}catch(n){i(n)}}function step(n){n.done?o(n.value):function adopt(n){return n instanceof e?n:new e((function(t){t(n)}))}(n.value).then(fulfilled,rejected)}step((r=r.apply(n,t||[])).next())}))},login_generator=function(n,t){var e,r,o,i,l={label:0,sent:function(){if(1&o[0])throw o[1];return o[1]},trys:[],ops:[]};return i={next:verb(0),throw:verb(1),return:verb(2)},"function"==typeof Symbol&&(i[Symbol.iterator]=function(){return this}),i;function verb(i){return function(a){return function step(i){if(e)throw new TypeError("Generator is already executing.");for(;l;)try{if(e=1,r&&(o=2&i[0]?r.return:i[0]?r.throw||((o=r.return)&&o.call(r),0):r.next)&&!(o=o.call(r,i[1])).done)return o;switch(r=0,o&&(i=[2&i[0],o.value]),i[0]){case 0:case 1:o=i;break;case 4:return l.label++,{value:i[1],done:!1};case 5:l.label++,r=i[1],i=[0];continue;case 7:i=l.ops.pop(),l.trys.pop();continue;default:if(!(o=l.trys,(o=o.length>0&&o[o.length-1])||6!==i[0]&&2!==i[0])){l=0;continue}if(3===i[0]&&(!o||i[1]>o[0]&&i[1]<o[3])){l.label=i[1];break}if(6===i[0]&&l.label<o[1]){l.label=o[1],o=i;break}if(o&&l.label<o[2]){l.label=o[2],l.ops.push(i);break}o[2]&&l.ops.pop(),l.trys.pop();continue}i=t.call(n,l)}catch(n){i=[6,n],r=0}finally{e=o=0}if(5&i[0])throw i[1];return{value:i[0]?i[1]:void 0,done:!0}}([i,a])}}},s=function(){function Login(n,t,e){this.authService=n,this.loginApiClient=t,this.router=e,this.msg=c.a}return Login.prototype.attached=function(){var n=this;this.loginApiClient.getCompanies().then((function(t){n.companies=t,t[0]&&n.getLocales(t[0])}))},Login.prototype.selectedCompanyChanged=function(n,t){t&&this.getLocales(n)},Login.prototype.getLocales=function(n){var t=this;this.loginApiClient.getLocalesFor(n).then((function(n){t.locales=n,t.selectedLocale=t.authService.getLocale()}))},Login.prototype.logIn=function(){return login_awaiter(this,void 0,void 0,(function(){return login_generator(this,(function(n){switch(n.label){case 0:return[4,this.authService.logIn(this.username,this.password,this.selectedCompany,this.selectedLocale)];case 1:return n.sent(),this.authService.authenticated&&(this.password=this.username="",this.router.navigateToRoute(this.router.routes[0].name)),[2]}}))}))},login_decorate([r.w,login_metadata("design:type",String)],Login.prototype,"selectedCompany",void 0),Login=login_decorate([r.m,login_metadata("design:paramtypes",[i.a,a,o.d])],Login)}()},"screens/Login/login.css":function(n,t,e){var r=e("denP"),o=e("JPst")(r);o.push([n.i,".login-form \r\n{ \r\n\tpadding: 12px; \r\n\tmax-width: 500px;\r\n}\r\n \r\n.login-form input,\r\n.login-form button,\r\n.login-form select \r\n{\r\n\tbox-sizing: border-box;\r\n\r\n\twidth: 100%;\r\n\tmargin: 6px 0;\r\n\tcolor: rgba(0, 0, 0, 0.64);\r\n\tfont-size: 19px;\r\n\tfont-weight: 500;\r\n\r\n\tpadding: 6px 8px;\r\n\tborder: solid 1px RGBA(0, 0, 0, 0.12);\r\n\tborder-radius: 3px;\r\n}\r\n\r\n.login-form #cmbLang\r\n{\r\n\tmargin-bottom: 32px;\r\n}\r\n.login-form #cmbCompany\r\n{\r\n\tmargin-top: 32px;\r\n}\r\n \r\n.login-form input::placeholder {\tfont-style: normal; }\r\n \r\n.login-form button \r\n{\r\n\tcolor: #444;\r\n\tcursor: pointer;\r\n\tpadding: 5px;\r\n\tbackground-color: #fff;\r\n\r\n\tborder: solid 1px #027ACC;\r\n\tborder-radius: 5px;\r\n\ttransition: all .1s ease-in-out;\r\n\t-webkit-appearance: none;\r\n}\r\n\r\n.login-form button:focus,\r\n.login-form button:hover \r\n{\r\n\tcolor: white;\r\n\tbackground-color: #027ACC;\r\n\tborder: solid 1px #027ACC;\r\n}\r\n\r\n.login-form button:disabled \r\n{\r\n\tcolor: #444;\r\n\tcursor: default;\r\n\tbackground-color: #eee;\r\n\tborder: solid 1px #ccc;\r\n}\r\n","",{version:3,sources:["webpack://./src/screens/Login/login.css"],names:[],mappings:"AAAA;;CAEC,aAAa;CACb,gBAAgB;AACjB;;AAEA;;;;CAIC,sBAAsB;;CAEtB,WAAW;CACX,aAAa;CACb,0BAA0B;CAC1B,eAAe;CACf,gBAAgB;;CAEhB,gBAAgB;CAChB,qCAAqC;CACrC,kBAAkB;AACnB;;AAEA;;CAEC,mBAAmB;AACpB;AACA;;CAEC,gBAAgB;AACjB;;AAEA,iCAAiC,kBAAkB,EAAE;;AAErD;;CAEC,WAAW;CACX,eAAe;CACf,YAAY;CACZ,sBAAsB;;CAEtB,yBAAyB;CACzB,kBAAkB;CAClB,+BAA+B;CAC/B,wBAAwB;AACzB;;AAEA;;;CAGC,YAAY;CACZ,yBAAyB;CACzB,yBAAyB;AAC1B;;AAEA;;CAEC,WAAW;CACX,eAAe;CACf,sBAAsB;CACtB,sBAAsB;AACvB",sourcesContent:[".login-form \r\n{ \r\n\tpadding: 12px; \r\n\tmax-width: 500px;\r\n}\r\n \r\n.login-form input,\r\n.login-form button,\r\n.login-form select \r\n{\r\n\tbox-sizing: border-box;\r\n\r\n\twidth: 100%;\r\n\tmargin: 6px 0;\r\n\tcolor: rgba(0, 0, 0, 0.64);\r\n\tfont-size: 19px;\r\n\tfont-weight: 500;\r\n\r\n\tpadding: 6px 8px;\r\n\tborder: solid 1px RGBA(0, 0, 0, 0.12);\r\n\tborder-radius: 3px;\r\n}\r\n\r\n.login-form #cmbLang\r\n{\r\n\tmargin-bottom: 32px;\r\n}\r\n.login-form #cmbCompany\r\n{\r\n\tmargin-top: 32px;\r\n}\r\n \r\n.login-form input::placeholder {\tfont-style: normal; }\r\n \r\n.login-form button \r\n{\r\n\tcolor: #444;\r\n\tcursor: pointer;\r\n\tpadding: 5px;\r\n\tbackground-color: #fff;\r\n\r\n\tborder: solid 1px #027ACC;\r\n\tborder-radius: 5px;\r\n\ttransition: all .1s ease-in-out;\r\n\t-webkit-appearance: none;\r\n}\r\n\r\n.login-form button:focus,\r\n.login-form button:hover \r\n{\r\n\tcolor: white;\r\n\tbackground-color: #027ACC;\r\n\tborder: solid 1px #027ACC;\r\n}\r\n\r\n.login-form button:disabled \r\n{\r\n\tcolor: #444;\r\n\tcursor: default;\r\n\tbackground-color: #eee;\r\n\tborder: solid 1px #ccc;\r\n}\r\n"],sourceRoot:""}]),n.exports=o},"screens/Login/login.html":function(n,t,e){n.exports='<template> <require from="./login.css"></require> <form class="login-form"> <select value.bind="selectedLocale" id="cmbLang" if.bind="locales && locales.length > 1"> <option repeat.for="locale of locales" model.bind="locale.Name"> ${locale.DisplayName} </option> </select> <br/><br/> <input type="text" name="username" value.bind="username" id="txtUser" required placeholder="My Username"> <input type="password" value.bind="password" id="txtPass" required placeholder="My Password"> <select value.bind="selectedCompany" id="cmbCompany" if.bind="companies && companies.length > 1"> <option repeat.for="company of companies" value.bind="company"> ${company} </option> </select> <button click.delegate="logIn()" disabled.bind="!username || !password" id="btnLogin" type="submit"> ${msg.SignIn} </button> </form> </template> '}}]);