/* Shared accessibility and submission behavior. Server validation stays authoritative. */
(()=>{'use strict';
 function tables(){
  for(const [i,dialog]of [...document.querySelectorAll('dialog')].entries()){
   if(dialog.hasAttribute('aria-labelledby')||dialog.hasAttribute('aria-label'))continue;
   const heading=dialog.querySelector('h1,h2,h3');if(!heading)continue;
   if(!heading.id)heading.id='p8-dialog-heading-'+i;dialog.setAttribute('aria-labelledby',heading.id);
  }
  for(const table of document.querySelectorAll('table')){
   const heads=[...table.querySelectorAll('thead tr:first-child th')];
   for(const h of heads)h.scope='col';
   if(table.closest('[data-dashboard]')&&heads.length<=6)table.classList.add('phase8-responsive');
   for(const row of table.querySelectorAll('tbody tr'))for(const [i,cell]of [...row.cells].entries()){
    if(cell.colSpan!==1)continue;
    if(!cell.dataset.label)cell.dataset.label=heads[i]?.textContent.trim()||'';
    if(cell.dataset.label==='Status'&&!cell.children.length&&cell.textContent.trim()){
     const badge=document.createElement('span');badge.className='kx-status';badge.dataset.status=cell.textContent.trim();badge.textContent=badge.dataset.status;cell.replaceChildren(badge);
    }
   }
   const scroll=table.closest('.tu-scroll');if(scroll){scroll.tabIndex=0;scroll.setAttribute('role','region');if(!scroll.hasAttribute('aria-label'))scroll.setAttribute('aria-label','Scrollable results table');}
  }
 }
 let pending=false;const observer=new MutationObserver(()=>{if(pending)return;pending=true;requestAnimationFrame(()=>{pending=false;observer.disconnect();tables();observer.observe(document.body,{childList:true,subtree:true});});});
 tables();observer.observe(document.body,{childList:true,subtree:true});
 // Lock native POSTs only after the browser has accepted the form. Preserve submitter values.
 const nativeForms=new WeakSet(),approvedForms=new WeakSet();
 document.addEventListener('submit',event=>{
  const form=event.target;if(form.method.toLowerCase()!=='post'||form.dataset.async!==undefined)return;
  if(nativeForms.has(form)){event.preventDefault();return;}
  const submitter=event.submitter;
  if(submitter&&/\/Pages\/Auth\/accounts\/(status|review)$/.test(new URL(form.action).pathname)&&['Inactive','reject'].includes(submitter.value)&&!approvedForms.has(form)){
   event.preventDefault();if(document.getElementById('p8AccountConfirm')?.open)return;
   const dialog=document.createElement('dialog');dialog.id='p8AccountConfirm';dialog.className='p8-confirm-dialog';dialog.setAttribute('aria-labelledby','p8AccountConfirmTitle');
   const rejecting=submitter.value==='reject';dialog.innerHTML='<h2 id="p8AccountConfirmTitle"></h2><p></p><div class="workspace-actions"><button type="button" class="dash-btn secondary" data-p8-keep>Keep account</button><button type="button" class="dash-btn danger" data-p8-confirm></button></div>';
   dialog.querySelector('h2').textContent=rejecting?'Reject this registration?':'Deactivate this account?';
   dialog.querySelector('p').textContent=rejecting?'The request will be rejected and will not receive account access.':'This will revoke existing sessions and block sign-in. The account and its history will be preserved.';
   dialog.querySelector('[data-p8-confirm]').textContent=rejecting?'Reject request':'Deactivate account';
   dialog.querySelector('[data-p8-keep]').onclick=()=>dialog.close();dialog.querySelector('[data-p8-confirm]').onclick=()=>dialog.close('confirm');
   dialog.addEventListener('close',()=>{const confirmed=dialog.returnValue==='confirm';dialog.remove();if(confirmed&&form.isConnected&&submitter.isConnected){approvedForms.add(form);form.requestSubmit(submitter);}});
   document.body.append(dialog);dialog.showModal();dialog.querySelector('[data-p8-keep]').focus();return;
  }
  approvedForms.delete(form);
  queueMicrotask(()=>{if(event.defaultPrevented)return;nativeForms.add(form);form.setAttribute('aria-busy','true');for(const b of form.querySelectorAll('button'))b.setAttribute('aria-disabled','true');});
 });
 addEventListener('pageshow',()=>{for(const form of document.forms){nativeForms.delete(form);form.removeAttribute('aria-busy');for(const b of form.querySelectorAll('[aria-disabled]'))b.removeAttribute('aria-disabled');}});
 document.addEventListener('cancel',event=>{if(event.target.tagName==='DIALOG')event.target.removeAttribute('aria-busy');},true);
})();
