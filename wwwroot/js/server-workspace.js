/* SQL dashboard ledgers, account pagination, site presentation and direct messages. */
(()=>{'use strict';
 const esc=v=>KicksStore.escape(String(v??''));
 const token=()=>JSON.parse(document.getElementById('kicks-auth-state').textContent).token;
 async function request(url,body){
  if(body)body.set('__RequestVerificationToken',token());
  const r=await fetch(url,{credentials:'same-origin',cache:'no-store',...(body?{method:'POST',body}:{})});
  if(r.redirected)throw Error('Your session has ended. Sign in again to continue.');
  if(!r.ok)throw Error(r.status===429?'Too many requests. Please wait a minute.':'The request could not be completed. Refresh and retry.');
  return (r.headers.get('content-type')||'').includes('json')?r.json():r.text();
 }
 let ledgerSearch='',accountsSearch='',accountStatus='all',panelVersion=0;
 async function panel(url,hash){
  const host=document.getElementById('dashboardView'),version=++panelVersion;
  host.innerHTML='<section class="p8-loading" role="status">Loading records…</section>';
  try{const html=await request(url);if(location.hash===hash&&version===panelVersion)host.innerHTML=html;}
  catch(e){if(location.hash!==hash||version!==panelVersion)return;host.innerHTML='<section class="p8-error" role="alert"><p></p><button class="dash-btn secondary">Retry</button></section>';host.querySelector('p').textContent=e.message;host.querySelector('button').onclick=()=>panel(url,hash);}
 }
 const ledger=(kind,page=1)=>panel('/dashboard/records/'+kind+'?'+new URLSearchParams({page,search:ledgerSearch}),'#'+kind);
 const accounts=(page=1)=>panel('/Pages/Auth/accounts?'+new URLSearchParams({page,search:accountsSearch,status:accountStatus}),'#users');
 const settings=()=>panel('/dashboard/settings','#settings');
 document.addEventListener('submit',async event=>{
  const form=event.target;
  if(form.matches('[data-workspace-filter]')){event.preventDefault();ledgerSearch=new FormData(form).get('Search')||'';ledger(document.getElementById('sqlWorkspaceRecords').dataset.kind);}
  if(form.id==='sqlAccountFilters'){event.preventDefault();const data=new FormData(form);accountsSearch=data.get('Search')||'';accountStatus=data.get('Status')||'all';accounts();}
  if(form.id==='sitePresentationForm'){
   event.preventDefault();const button=form.querySelector('button');if(button.disabled)return;button.disabled=true;const notice=form.querySelector('[role=status]');notice.textContent='Saving…';
   try{notice.textContent=(await request('/dashboard/settings',new FormData(form))).message;}catch(e){notice.textContent=e.message;}finally{button.disabled=false;}
  }
 });
 document.addEventListener('click',event=>{const b=event.target.closest('button');if(!b)return;if(b.dataset.workspacePage)ledger(document.getElementById('sqlWorkspaceRecords').dataset.kind,Number(b.dataset.workspacePage));if(b.dataset.accountPage)accounts(Number(b.dataset.accountPage));});

 let selected=null,contactPage=1,contactSearch='',messageBusy=false,renderVersion=0;
 const drafts=new Map();
 function messageRows(items){
  const actor=KicksAuth.current().id;
  return items.map(m=>`<article class="p8-bubble ${m.senderId===actor?'outgoing':'incoming'}"><p>${esc(m.body)}</p><small>${esc(new Date(m.createdAt).toLocaleString('en-GB',{timeZone:'Asia/Amman',dateStyle:'medium',timeStyle:'short'}))}${m.senderId===actor?(m.readAt?' · Read':' · Sent'):''}</small></article>`).join('');
 }
 async function messages(){
  const host=document.getElementById('messagesPanel');if(!host)return;const version=++renderVersion;
  host.innerHTML='<div class="p8-loading" role="status">Loading conversations…</div>';
  try{
   const contacts=await request('/api/messages/contacts?'+new URLSearchParams({page:contactPage,search:contactSearch}));
   if(version!==renderVersion||!host.isConnected)return;
   const person=contacts.items.find(p=>p.id===selected);if(!person)selected=null;
   const thread=person?await request('/api/messages/'+person.id):{items:[]};
   if(version!==renderVersion||!host.isConnected)return;
   host.innerHTML=`<div class="panel-heading"><div><h2>Direct messages</h2><p>Your booking contacts, opposing captains and the KICKS administrator.</p></div></div>
    <div class="p8-messages"><aside class="p8-contacts"><h3>Conversations</h3>
    <form id="messageContactSearch"><label for="messageSearch">Find a contact</label><input id="messageSearch" type="search" maxlength="100" value="${esc(contactSearch)}"><button class="dash-btn secondary">Search</button></form>
    <div>${contacts.items.map(p=>`<button type="button" data-message-person="${p.id}" aria-pressed="${p.id===selected}">${esc(p.name)}</button>`).join('')||'<p>No eligible contacts yet. Contacts appear after a booking or team challenge.</p>'}</div>
    <nav class="kicks-table-pagination" aria-label="Contact pages">${contacts.page>1?'<button data-contact-page="'+(contacts.page-1)+'">Previous</button>':''}${contacts.page*20<contacts.total?'<button data-contact-page="'+(contacts.page+1)+'">Next</button>':''}</nav></aside>
    <section class="p8-thread">${person?`<h3>${esc(person.name)}</h3><div class="p8-history" tabindex="0" role="log" aria-label="Message history">${thread.hasOlder?'<button class="dash-btn secondary" id="olderMessages">Load earlier messages</button>':''}<div id="messageRows">${messageRows(thread.items)||'<p>Say hello to start the conversation.</p>'}</div></div>
    <form id="sqlMessageForm"><label for="messageText">Your message</label><textarea id="messageText" maxlength="2000" rows="3" required placeholder="Write a message…">${esc(drafts.get(selected)||'')}</textarea><button class="dash-btn" type="submit">Send message ↗</button><p id="messageError" role="status" aria-live="polite"></p></form>`:'<div class="phase8-empty"><span aria-hidden="true">✉</span><h3>Your conversations, together</h3><p>Choose a contact to read or send a message. Messages are saved securely to your account.</p></div>'}</section></div>`;
   for(const b of host.querySelectorAll('[data-message-person]'))b.onclick=()=>{selected=Number(b.dataset.messagePerson);messages();};
   for(const b of host.querySelectorAll('[data-contact-page]'))b.onclick=()=>{contactPage=Number(b.dataset.contactPage);messages();};
   host.querySelector('#messageContactSearch').onsubmit=e=>{e.preventDefault();contactSearch=host.querySelector('#messageSearch').value.trim();contactPage=1;messages();};
   if(!person)return;
   let before=thread.items[0]?.id;
   const older=host.querySelector('#olderMessages');
   if(older)older.onclick=async()=>{older.disabled=true;try{const data=await request('/api/messages/'+person.id+'?before='+before);host.querySelector('#messageRows').insertAdjacentHTML('afterbegin',messageRows(data.items));before=data.items[0]?.id??before;older.hidden=!data.hasOlder;}catch(e){host.querySelector('#messageError').textContent=e.message;}finally{older.disabled=false;}};
   request('/messages/'+person.id+'/read',new FormData()).catch(()=>{});
   const form=host.querySelector('#sqlMessageForm'),input=host.querySelector('#messageText');
   input.oninput=()=>drafts.set(person.id,input.value);
   form.onsubmit=async e=>{
    e.preventDefault();if(messageBusy||!form.reportValidity())return;messageBusy=true;const b=form.querySelector('button');b.disabled=true;
    const body=new FormData();body.set('RecipientId',person.id);body.set('Body',input.value);
    try{await request('/messages',body);drafts.delete(person.id);await messages();host.querySelector('#messageText')?.focus();}
    catch(error){host.querySelector('#messageError').textContent=error.message;}finally{messageBusy=false;if(b.isConnected)b.disabled=false;}
   };
   const history=host.querySelector('.p8-history');history.scrollTop=history.scrollHeight;
  }catch(e){if(version!==renderVersion||!host.isConnected)return;host.innerHTML='<div class="p8-error" role="alert"><p></p><button class="dash-btn secondary">Retry conversations</button></div>';host.querySelector('p').textContent=e.message;host.querySelector('button').onclick=messages;}
 }
 window.KicksWorkspace={ledger,accounts,settings,messages};
})();
