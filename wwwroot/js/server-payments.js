// SQL status reflects verified webhooks only. Redirects and browser storage never fulfil a payment.
(()=>{'use strict';const root=document.querySelector('[data-payment-id]');if(!root)return;
 for(const form of root.querySelectorAll('form'))form.addEventListener('submit',()=>{for(const b of root.querySelectorAll('button'))b.disabled=true;});
 window.addEventListener('pageshow',()=>{for(const b of root.querySelectorAll('button'))b.disabled=false;});
 const timer=setInterval(async()=>{try{const r=await fetch('/api/payments/'+root.dataset.paymentId,{credentials:'same-origin',cache:'no-store'});if(!r.ok||r.redirected)return;const p=await r.json();document.getElementById('paymentStatus').textContent=p.status;
 if(p.status!=='Pending'||!p.holdActive){clearInterval(timer);document.getElementById('paymentActions')?.remove();document.getElementById('paymentNotice').textContent=p.financialReview?'Payment received; manual financial review required. No automatic refund.':p.status==='Paid'?'Payment verified by Stripe webhook. Your place is confirmed.':'The hold has closed. No reservation was confirmed by this page.';}}catch{}},2000);
})();
