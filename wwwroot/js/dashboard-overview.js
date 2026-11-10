/* SQL-backed workspace overview. Browser storage is never a data source. */
(()=>{'use strict';let generation=0;
 async function render(){
  const target=document.getElementById('dashboardView'),current=++generation;
  target.innerHTML='<section class="p8-loading" role="status">Loading your workspace…</section>';
  try{
   const response=await fetch('/dashboard/overview',{credentials:'same-origin',cache:'no-store'});
   if(!response.ok||response.redirected)throw Error('Your workspace could not be loaded. Sign in again or retry.');
   const html=await response.text();if(current!==generation||!['','#overview'].includes(location.hash))return;
   target.innerHTML=html;target.dataset.organized='true';
  }catch(error){if(current!==generation||!['','#overview'].includes(location.hash))return;target.replaceChildren();const panel=document.createElement('section');panel.className='p8-error';panel.setAttribute('role','alert');const text=document.createElement('p');text.textContent=error.message;const retry=document.createElement('button');retry.className='dash-btn secondary';retry.textContent='Retry workspace';retry.onclick=render;panel.append(text,retry);target.append(panel);}
 }
 window.KicksDashboardOverview={render};
})();
