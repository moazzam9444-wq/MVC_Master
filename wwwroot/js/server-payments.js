// SQL status reflects verified webhooks only. Browser redirects never fulfil a payment.
(()=>{'use strict';const root=document.querySelector('[data-payment-id]');if(!root)return;
 const notice=document.getElementById('paymentNotice');let busy=false,failures=0,checks=0;
 for(const form of root.querySelectorAll('form'))form.addEventListener('submit',()=>{for(const b of root.querySelectorAll('button'))b.disabled=true;});
 addEventListener('pageshow',()=>{for(const b of root.querySelectorAll('button'))b.disabled=false;});
 const timer=setInterval(async()=>{
  if(busy||document.hidden)return;busy=true;
  try{
   const r=await fetch('/api/payments/'+root.dataset.paymentId,{credentials:'same-origin',cache:'no-store'});
   if(r.redirected||r.status===401||r.status===403){clearInterval(timer);notice.textContent='Your session has ended. Sign in again to review this payment. Payment has not been confirmed by this page.';return;}
   if(!r.ok)throw Error();const p=await r.json();failures=0;checks++;document.getElementById('paymentStatus').textContent=p.status;
   if(p.status!=='Pending'||!p.holdActive){
    document.getElementById('paymentActions')?.remove();
    notice.textContent=p.financialReview?'Payment received; manual financial review required. No automatic refund.':p.status==='Paid'?'Payment verified by Stripe webhook. Your place is confirmed.':'The hold has closed. This page has not confirmed a reservation. Return later to check for any delayed payment update.';
    if(p.status==='Paid'||checks>=150)clearInterval(timer);
   }else if(checks>=10)notice.textContent='Payment verification is still pending. If you already paid, keep this page open or return later; do not pay again.';
  }catch{failures++;if(failures>=3)notice.textContent='Connection interrupted. Your payment status has not been updated. We will retry automatically; you can also refresh this page.';}
  finally{busy=false;}
 },2000);
})();