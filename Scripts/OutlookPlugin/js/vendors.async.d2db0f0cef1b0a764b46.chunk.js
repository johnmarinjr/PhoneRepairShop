(window.webpackJsonp=window.webpackJsonp||[]).push([[0],{"2B/d":function(t,e,o){"use strict";o.r(e),o.d(e,"DialogRenderer",(function(){return g})),o.d(e,"UxDialogRenderer",(function(){return g})),o.d(e,"hasTransition",(function(){return c})),o.d(e,"transitionEvent",(function(){return transitionEvent}));var i,n,r,a,l,s=o("oP5Z"),d=o("k6aC"),transitionEvent=function(){if(i)return i;var t=s.b.createElement("fakeelement"),e={transition:"transitionend",OTransition:"oTransitionEnd",MozTransition:"transitionend",WebkitTransition:"webkitTransitionEnd"};for(var o in e)if(void 0!==t.style[o])return i=e[o];return""},c=(a=["webkitTransitionDuration","oTransitionDuration"],function(t){return n||(n=s.b.createElement("fakeelement"),r="transitionDuration"in n.style?"transitionDuration":a.find((function(t){return t in n.style}))),!!r&&!!s.b.getComputedStyle(t)[r].split(",").find((function(t){return!!parseFloat(t)}))});var g=function(){function DialogRenderer(){}return DialogRenderer.keyboardEventHandler=function(t){var e=function getActionKey(t){return"Escape"===(t.code||t.key)||27===t.keyCode?"Escape":"Enter"===(t.code||t.key)||13===t.keyCode?"Enter":void 0}(t);if(e){var o=DialogRenderer.dialogControllers[DialogRenderer.dialogControllers.length-1];if(o&&o.settings.keyboard){var i=o.settings.keyboard;"Escape"===e&&(!0===i||i===e||Array.isArray(i)&&i.indexOf(e)>-1)?o.cancel():"Enter"===e&&(i===e||Array.isArray(i)&&i.indexOf(e)>-1)&&o.ok()}}},DialogRenderer.trackController=function(t){var e=DialogRenderer.dialogControllers;e.length||s.b.addEventListener(t.settings.keyEvent||"keyup",DialogRenderer.keyboardEventHandler,!1),e.push(t)},DialogRenderer.untrackController=function(t){var e=DialogRenderer.dialogControllers,o=e.indexOf(t);-1!==o&&e.splice(o,1),e.length||s.b.removeEventListener(t.settings.keyEvent||"keyup",DialogRenderer.keyboardEventHandler,!1)},DialogRenderer.prototype.getOwnElements=function(t,e){for(var o=t.querySelectorAll(e),i=[],n=0;n<o.length;n++)o[n].parentElement===t&&i.push(o[n]);return i},DialogRenderer.prototype.attach=function(t){t.settings.restoreFocus&&(this.lastActiveElement=s.b.activeElement);var e=s.b.createElement("div");e.appendChild(this.anchor);var o=this.dialogContainer=s.b.createElement("ux-dialog-container");o.appendChild(e);var i=this.dialogOverlay=s.b.createElement("ux-dialog-overlay"),n="number"==typeof t.settings.startingZIndex?t.settings.startingZIndex+"":"auto";i.style.zIndex=n,o.style.zIndex=n;var r=this.host,a=this.getOwnElements(r,"ux-dialog-container").pop();a&&a.parentElement?(r.insertBefore(o,a.nextSibling),r.insertBefore(i,a.nextSibling)):(r.insertBefore(o,r.firstChild),r.insertBefore(i,r.firstChild)),t.controller.attached(),r.classList.add("ux-dialog-open")},DialogRenderer.prototype.detach=function(t){var e=this.host;e.removeChild(this.dialogOverlay),e.removeChild(this.dialogContainer),t.controller.detached(),DialogRenderer.dialogControllers.length||e.classList.remove("ux-dialog-open"),t.settings.restoreFocus&&t.settings.restoreFocus(this.lastActiveElement)},DialogRenderer.prototype.setAsActive=function(){this.dialogOverlay.classList.add("active"),this.dialogContainer.classList.add("active")},DialogRenderer.prototype.setAsInactive=function(){this.dialogOverlay.classList.remove("active"),this.dialogContainer.classList.remove("active")},DialogRenderer.prototype.setupEventHandling=function(t){this.stopPropagation=function(t){t._aureliaDialogHostClicked=!0},this.closeDialogClick=function(e){t.settings.overlayDismiss&&!e._aureliaDialogHostClicked&&t.cancel()};var e=t.settings.mouseEvent||"click";this.dialogContainer.addEventListener(e,this.closeDialogClick),this.anchor.addEventListener(e,this.stopPropagation)},DialogRenderer.prototype.clearEventHandling=function(t){var e=t.settings.mouseEvent||"click";this.dialogContainer.removeEventListener(e,this.closeDialogClick),this.anchor.removeEventListener(e,this.stopPropagation)},DialogRenderer.prototype.centerDialog=function(){var t=this.dialogContainer.children[0],e=Math.max(s.b.querySelectorAll("html")[0].clientHeight,window.innerHeight||0);t.style.marginTop=Math.max((e-t.offsetHeight)/2,30)+"px",t.style.marginBottom=Math.max((e-t.offsetHeight)/2,30)+"px"},DialogRenderer.prototype.awaitTransition=function(t,e){var o=this;return new Promise((function(i){var n=o,r=transitionEvent();e||!c(o.dialogContainer)?i():o.dialogContainer.addEventListener(r,(function onTransitionEnd(t){t.target===n.dialogContainer&&(n.dialogContainer.removeEventListener(r,onTransitionEnd),i())})),t()}))},DialogRenderer.prototype.getDialogContainer=function(){return this.anchor||(this.anchor=s.b.createElement("div"))},DialogRenderer.prototype.showDialog=function(t){var e=this;l||(l=s.b.querySelector("body")),t.settings.host?this.host=t.settings.host:this.host=l;var o=t.settings;return this.attach(t),"function"==typeof o.position?o.position(this.dialogContainer,this.dialogOverlay):o.centerHorizontalOnly||this.centerDialog(),DialogRenderer.trackController(t),this.setupEventHandling(t),this.awaitTransition((function(){return e.setAsActive()}),t.settings.ignoreTransitions)},DialogRenderer.prototype.hideDialog=function(t){var e=this;return this.clearEventHandling(t),DialogRenderer.untrackController(t),this.awaitTransition((function(){return e.setAsInactive()}),t.settings.ignoreTransitions).then((function(){e.detach(t)}))},DialogRenderer.dialogControllers=[],DialogRenderer}();Object(d.f)()(g)},Do7W:function(t,e,o){"use strict";o.r(e),o.d(e,"UxDialogFooter",(function(){return n}));var i=o("tvlN"),n=function(){function UxDialogFooter(t){this.controller=t,this.buttons=[],this.useDefaultButtons=!1}return UxDialogFooter.isCancelButton=function(t){return"Cancel"===t},UxDialogFooter.prototype.close=function(t){UxDialogFooter.isCancelButton(t)?this.controller.cancel(t):this.controller.ok(t)},UxDialogFooter.prototype.useDefaultButtonsChanged=function(t){t&&(this.buttons=["Cancel","Ok"])},UxDialogFooter.inject=[i.d],UxDialogFooter.$view='<template>\n  <slot></slot>\n  <template if.bind="buttons.length > 0">\n    <button type="button"\n      class="btn btn-default"\n      repeat.for="button of buttons"\n      click.trigger="close(button)">\n      ${button}\n    </button>\n  </template>\n</template>',UxDialogFooter.$resource={name:"ux-dialog-footer",bindables:["buttons","useDefaultButtons"]},UxDialogFooter}()},Dreq:function(t,e,o){"use strict";o.r(e),o.d(e,"UxDialogHeader",(function(){return n}));var i=o("tvlN"),n=function(){function UxDialogHeader(t){this.controller=t}return UxDialogHeader.prototype.bind=function(){"boolean"!=typeof this.showCloseButton&&(this.showCloseButton=!this.controller.settings.lock)},UxDialogHeader.inject=[i.d],UxDialogHeader.$view='<template>\n  <button\n    type="button"\n    class="dialog-close"\n    aria-label="Close"\n    if.bind="showCloseButton"\n    click.trigger="controller.cancel()">\n    <span aria-hidden="true">&times;</span>\n  </button>\n\n  <div class="dialog-header-content">\n    <slot></slot>\n  </div>\n</template>',UxDialogHeader.$resource={name:"ux-dialog-header",bindables:["showCloseButton"]},UxDialogHeader}()},HqgY:function(t,e,o){"use strict";o.r(e);e.default="ux-dialog-overlay{bottom:0;left:0;position:fixed;top:0;right:0;opacity:0}ux-dialog-overlay.active{opacity:1}ux-dialog-container{display:block;transition:opacity .2s linear;opacity:0;overflow-x:hidden;overflow-y:auto;position:fixed;top:0;right:0;bottom:0;left:0;-webkit-overflow-scrolling:touch}ux-dialog-container.active{opacity:1}ux-dialog-container>div{padding:30px}ux-dialog-container>div>div{width:100%;display:block;min-width:300px;width:-moz-fit-content;width:-webkit-fit-content;width:fit-content;height:-moz-fit-content;height:-webkit-fit-content;height:fit-content;margin:auto}ux-dialog-container,ux-dialog-container>div,ux-dialog-container>div>div{outline:0}ux-dialog{width:100%;display:table;box-shadow:0 5px 15px rgba(0,0,0,.5);border:1px solid rgba(0,0,0,.2);border-radius:5px;padding:3;min-width:300px;width:-moz-fit-content;width:-webkit-fit-content;width:fit-content;height:-moz-fit-content;height:-webkit-fit-content;height:fit-content;margin:auto;border-image-source:none;border-image-slice:100%;border-image-width:1;border-image-outset:0;border-image-repeat:initial;background:#fff}ux-dialog>ux-dialog-header{display:block;padding:16px;border-bottom:1px solid #e5e5e5}ux-dialog>ux-dialog-header>button{float:right;border:none;display:block;width:32px;height:32px;background:none;font-size:22px;line-height:16px;margin:-14px -16px 0 0;padding:0;cursor:pointer}ux-dialog>ux-dialog-body{display:block;padding:16px}ux-dialog>ux-dialog-footer{display:block;padding:6px;border-top:1px solid #e5e5e5;text-align:right}ux-dialog>ux-dialog-footer button{color:#333;background-color:#fff;padding:6px 12px;font-size:14px;text-align:center;white-space:nowrap;vertical-align:middle;-ms-touch-action:manipulation;touch-action:manipulation;cursor:pointer;background-image:none;border:1px solid #ccc;border-radius:4px;margin:5px 0 5px 5px}ux-dialog>ux-dialog-footer button:disabled{cursor:default;opacity:.45}ux-dialog>ux-dialog-footer button:hover:enabled{color:#333;background-color:#e6e6e6;border-color:#adadad}.ux-dialog-open{overflow:hidden}"},YRE1:function(t,e,o){"use strict";o.r(e),o.d(e,"AttachFocus",(function(){return n}));var i=o("oP5Z"),n=function(){function AttachFocus(t){this.element=t,this.value=!0}return AttachFocus.inject=function(){return[i.b.Element]},AttachFocus.prototype.attached=function(){(""===this.value||this.value&&"false"!==this.value)&&this.element.focus()},AttachFocus.$resource={type:"attribute",name:"attach-focus"},AttachFocus}()},cdO6:function(t,e,o){"use strict";o.r(e),o.d(e,"UxDialog",(function(){return i}));var i=function(){function UxDialog(){}return UxDialog.$view="<template><slot></slot></template>",UxDialog.$resource="ux-dialog",UxDialog}()},l18j:function(t,e,o){"use strict";o.r(e),o.d(e,"UxDialogBody",(function(){return i}));var i=function(){function UxDialogBody(){}return UxDialogBody.$view="<template><slot></slot></template>",UxDialogBody.$resource="ux-dialog-body",UxDialogBody}()},"o+D4":function(t,e,o){"use strict";o.r(e),o.d(e,"NativeDialogRenderer",(function(){return l}));var i=o("oP5Z"),n=o("k6aC"),r=o("2B/d");var a,l=function(){function NativeDialogRenderer(){}var t;return t=NativeDialogRenderer,NativeDialogRenderer.keyboardEventHandler=function(e){var o="Enter"===(e.code||e.key)||13===e.keyCode?"Enter":void 0;if(o){var i=t.dialogControllers[t.dialogControllers.length-1];if(i&&i.settings.keyboard){var n=i.settings.keyboard;"Enter"===o&&(n===o||Array.isArray(n)&&n.indexOf(o)>-1)&&i.ok()}}},NativeDialogRenderer.trackController=function(e){t.dialogControllers.length||i.b.addEventListener("keyup",t.keyboardEventHandler,!1),t.dialogControllers.push(e)},NativeDialogRenderer.untrackController=function(e){var o=t.dialogControllers.indexOf(e);-1!==o&&t.dialogControllers.splice(o,1),t.dialogControllers.length||i.b.removeEventListener("keyup",t.keyboardEventHandler,!1)},NativeDialogRenderer.prototype.getOwnElements=function(t,e){for(var o=t.querySelectorAll(e),i=[],n=0;n<o.length;n++)o[n].parentElement===t&&i.push(o[n]);return i},NativeDialogRenderer.prototype.attach=function(t){t.settings.restoreFocus&&(this.lastActiveElement=i.b.activeElement);var e=i.b.createElement("div");e.appendChild(this.anchor),this.dialogContainer=i.b.createElement("dialog"),window.dialogPolyfill&&window.dialogPolyfill.registerDialog(this.dialogContainer),this.dialogContainer.appendChild(e);var o=this.getOwnElements(this.host,"dialog").pop();o&&o.parentElement?this.host.insertBefore(this.dialogContainer,o.nextSibling):this.host.insertBefore(this.dialogContainer,this.host.firstChild),t.controller.attached(),this.host.classList.add("ux-dialog-open")},NativeDialogRenderer.prototype.detach=function(e){this.dialogContainer.hasAttribute("open")&&this.dialogContainer.close(),this.host.removeChild(this.dialogContainer),e.controller.detached(),t.dialogControllers.length||this.host.classList.remove("ux-dialog-open"),e.settings.restoreFocus&&e.settings.restoreFocus(this.lastActiveElement)},NativeDialogRenderer.prototype.setAsActive=function(){this.dialogContainer.showModal(),this.dialogContainer.classList.add("active")},NativeDialogRenderer.prototype.setAsInactive=function(){this.dialogContainer.classList.remove("active")},NativeDialogRenderer.prototype.setupEventHandling=function(t){this.stopPropagation=function(t){t._aureliaDialogHostClicked=!0},this.closeDialogClick=function(e){t.settings.overlayDismiss&&!e._aureliaDialogHostClicked&&t.cancel()},this.dialogCancel=function(e){var o=t.settings.keyboard;!0===o||"Escape"===o||Array.isArray(o)&&o.indexOf("Escape")>-1?t.cancel():e.preventDefault()};var e=t.settings.mouseEvent||"click";this.dialogContainer.addEventListener(e,this.closeDialogClick),this.dialogContainer.addEventListener("cancel",this.dialogCancel),this.anchor.addEventListener(e,this.stopPropagation)},NativeDialogRenderer.prototype.clearEventHandling=function(t){var e=t.settings.mouseEvent||"click";this.dialogContainer.removeEventListener(e,this.closeDialogClick),this.dialogContainer.removeEventListener("cancel",this.dialogCancel),this.anchor.removeEventListener(e,this.stopPropagation)},NativeDialogRenderer.prototype.awaitTransition=function(t,e){var o=this;return new Promise((function(i){var n=o,a=Object(r.transitionEvent)();e||!Object(r.hasTransition)(o.dialogContainer)?i():o.dialogContainer.addEventListener(a,(function onTransitionEnd(t){t.target===n.dialogContainer&&(n.dialogContainer.removeEventListener(a,onTransitionEnd),i())})),t()}))},NativeDialogRenderer.prototype.getDialogContainer=function(){return this.anchor||(this.anchor=i.b.createElement("div"))},NativeDialogRenderer.prototype.showDialog=function(e){var o=this;a||(a=i.b.querySelector("body")),e.settings.host?this.host=e.settings.host:this.host=a;var n=e.settings;return this.attach(e),"function"==typeof n.position&&n.position(this.dialogContainer),t.trackController(e),this.setupEventHandling(e),this.awaitTransition((function(){return o.setAsActive()}),e.settings.ignoreTransitions)},NativeDialogRenderer.prototype.hideDialog=function(e){var o=this;return this.clearEventHandling(e),t.untrackController(e),this.awaitTransition((function(){return o.setAsInactive()}),e.settings.ignoreTransitions).then((function(){o.detach(e)}))},NativeDialogRenderer.dialogControllers=[],NativeDialogRenderer=t=
/*! *****************************************************************************
Copyright (c) Microsoft Corporation. All rights reserved.
Licensed under the Apache License, Version 2.0 (the "License"); you may not use
this file except in compliance with the License. You may obtain a copy of the
License at http://www.apache.org/licenses/LICENSE-2.0

THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
MERCHANTABLITY OR NON-INFRINGEMENT.

See the Apache Version 2.0 License for specific language governing permissions
and limitations under the License.
***************************************************************************** */
function __decorate(t,e,o,i){var n,r=arguments.length,a=r<3?e:null===i?i=Object.getOwnPropertyDescriptor(e,o):i;if("object"==typeof Reflect&&"function"==typeof Reflect.decorate)a=Reflect.decorate(t,e,o,i);else for(var l=t.length-1;l>=0;l--)(n=t[l])&&(a=(r<3?n(a):r>3?n(e,o,a):n(e,o))||a);return r>3&&a&&Object.defineProperty(e,o,a),a}([Object(n.f)()],NativeDialogRenderer)}()}}]);