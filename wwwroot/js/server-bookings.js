/* SQL booking transport and UI. No prototype booking/payment storage is used. */
(() => {
 const esc=value=>KicksStore.escape(String(value??''));
 const token=()=>JSON.parse(document.getElementById('kicks-auth-state').textContent).token;
 async function request(url,options={}){
  const response=await fetch(url,{cache:'no-store',credentials:'same-origin',...options});
  if(response.redirected)throw Error('Please sign in with the appropriate account.');
  const json=(response.headers.get('content-type')||'').includes('json');const data=json?await response.json():await response.text();
  if(!response.ok)throw Error(data.message||'The request could not be completed. Refresh and retry.');return data;
 }
 const local=value=>new Date(value).toLocaleString('en-GB',{timeZone:'Asia/Amman',dateStyle:'medium',timeStyle:'short'});
 const summary={items:[],total:0};let filter={Page:1,Period:'all',Search:''};
 async function refreshSummary(){try{Object.assign(summary,await request('/bookings/data'));window.dispatchEvent(new Event('kicks-data-change'));}catch{}}
 async function render(changes={}){
  Object.assign(filter,changes);const target=document.getElementById('dashboardView');target.innerHTML='<section class="dash-panel"><p>Loading reservations…</p></section>';
  try{const html=await request('/bookings?'+new URLSearchParams(filter));if(location.hash==='#bookings')target.innerHTML=html;}catch(error){if(location.hash==='#bookings')target.textContent=error.message;}
 }
 function dialog(html){document.getElementById('sqlBookingDialog')?.remove();const node=document.createElement('dialog');node.id='sqlBookingDialog';node.innerHTML=html;document.body.append(node);node.querySelector('[data-booking-close]').onclick=()=>node.close();node.showModal();return node;}
 document.addEventListener('submit',event=>{if(event.target.id!=='sqlBookingFilters')return;event.preventDefault();render({...Object.fromEntries(new FormData(event.target)),Page:1});});
 document.addEventListener('click',async event=>{const b=event.target.closest('button');if(!b)return;
  if(b.dataset.bookingPage)render({Page:b.dataset.bookingPage});
  if(b.dataset.bookingView){try{const value=await request('/bookings/'+b.dataset.bookingView);dialog(`<header class="dialog-header"><h2>Reservation #${value.id}</h2><button data-booking-close aria-label="Close">×</button></header><h3>${esc(value.fieldName)}</h3><p>${esc(value.area)}, ${esc(value.city)}</p><p>${esc(local(value.startsAt))} · 60 minutes · Amman</p><p>${Number(value.price).toFixed(2)} JOD · ${esc(value.status)}</p><p>Payment settlement is deferred. No payment is collected here.</p>${value.cancelledAt?'<p>Cancelled '+esc(local(value.cancelledAt))+'</p>':''}`);}catch(error){KicksAuth.notify(error.message);}}
  if(b.dataset.bookingAction){const action=b.dataset.bookingAction;const node=dialog(`<header class="dialog-header"><h2>${action==='cancel'?'Cancel reservation?':'Complete reservation?'}</h2><button data-booking-close aria-label="Close">×</button></header><p>${action==='cancel'?'This releases the time slot and retains the booking history. No refund or payment record is changed.':'Confirm that this session has ended. The server checks its end time.'}</p><p role="alert" class="editor-error"></p><button class="dash-btn" id="sqlBookingConfirm">Confirm ${action}</button>`);node.querySelector('#sqlBookingConfirm').onclick=async()=>{const confirm=node.querySelector('#sqlBookingConfirm');confirm.disabled=true;const body=new FormData();body.set('__RequestVerificationToken',token());body.set('Version',b.dataset.version);try{await request(`/bookings/${b.dataset.id}/${action}`,{method:'POST',body});node.close();refreshSummary();render();}catch(error){node.querySelector('[role=alert]').textContent=error.message;}finally{confirm.disabled=false;}};}
 });
 function mount(field){
  const get=id=>document.getElementById(id),form=get('bookingForm'),date=get('bookingDate'),button=get('bookButton'),slots=get('timeSlots'),notice=get('slotStatus');let selected=null,quote=null,busy=false,generation=0;
  const today=()=>{const parts=Object.fromEntries(new Intl.DateTimeFormat('en-GB',{timeZone:'Asia/Amman',year:'numeric',month:'2-digit',day:'2-digit'}).formatToParts(new Date()).map(p=>[p.type,p.value]));return `${parts.year}-${parts.month}-${parts.day}`;};
  const player=()=>KicksAuth.current()?.role==='player';
  date.disabled=false;date.min=today();date.value=today();button.textContent=KicksAuth.current()?'Reserve this session':'Log in to reserve';
  document.querySelector('.kx-payment-caption').textContent='Reserve now. No payment is collected; settlement is deferred.';
  async function refresh(message=''){
   const version=++generation;selected=null;quote=null;button.disabled=!!KicksAuth.current();get('selectedTime').textContent='—';get('bookingTotal').textContent='—';notice.textContent='Checking live availability…';slots.replaceChildren();
   if(!date.value||!date.validity.valid){notice.textContent='Choose today or a future date.';return;}
   try{const result=await request(`/api/fields/${field.id}/availability?date=${encodeURIComponent(date.value)}`);if(version!==generation)return;
    for(const slot of result.slots){const label=document.createElement('label'),input=document.createElement('input'),text=document.createElement('span');input.type='radio';input.name='slot';input.value=slot.hour;input.disabled=!slot.available;text.textContent=slot.time;input.setAttribute('aria-label',slot.time+(slot.available?'':', unavailable'));label.append(input,text);slots.append(label);}
    notice.textContent=message||(KicksAuth.current()&&!player()?'Use a player account to reserve.':`${result.slots.filter(s=>s.available).length} one-hour slots available · Amman.`);
   }catch(error){notice.textContent=error.message;}
  }
  slots.onchange=event=>{selected=Number(event.target.value);get('selectedTime').textContent=`${selected}:00–${selected+1}:00`;get('bookingTotal').textContent=Number(field.price).toFixed(2)+' JOD';button.disabled=!!KicksAuth.current()&&!player();};
  date.onchange=()=>refresh();date.oninput=()=>refresh();
  form.onsubmit=async event=>{event.preventDefault();if(!KicksAuth.current()){location.assign(KicksAuth.loginUrl());return;}if(!player()||selected===null||busy)return;busy=true;button.disabled=true;
   try{quote=await request('/bookings/quote?'+new URLSearchParams({FieldId:field.id,Date:date.value,Hour:selected,DurationMinutes:60}));get('paymentAmount').textContent=Number(quote.price).toFixed(2)+' JOD';get('paymentDialogTitle').textContent='Confirm reservation';get('completeDemoPayment').textContent='Confirm reservation';get('bookingTotal').textContent=Number(quote.price).toFixed(2)+' JOD';get('bookingQuoteSummary').textContent=`${quote.fieldName} · ${local(quote.startsAt)} · 60 minutes · No payment collected.`;KicksDialogs.open(get('paymentDialog'));}
   catch(error){await refresh(error.message);}finally{busy=false;button.disabled=selected===null;}
  };
  get('completeDemoPayment').onclick=async()=>{if(busy||!quote)return;busy=true;const confirm=get('completeDemoPayment');confirm.disabled=true;const body=new FormData();body.set('QuoteToken',quote.quoteToken);body.set('__RequestVerificationToken',token());
   try{const result=await request('/bookings',{method:'POST',body});get('paymentDialog').close();get('bookingDialogTitle').textContent='Reservation '+result.booking.status.toLowerCase();get('bookingPreview').textContent=`Reservation #${result.booking.id} · ${field.name} · ${local(result.booking.startsAt)} · 60 minutes · ${Number(result.booking.price).toFixed(2)} JOD. ${result.booking.status}; no payment collected.`;KicksDialogs.open(get('bookingDialog'));await refresh();}
   catch(error){get('paymentDialog').close();await refresh(error.message);}finally{busy=false;confirm.disabled=false;}
  };
  refresh();
 }
 window.KicksBookings={mount,render,summary,refreshSummary};
 if(['owner','player'].includes(KicksAuth.current()?.role))refreshSummary();
})();
