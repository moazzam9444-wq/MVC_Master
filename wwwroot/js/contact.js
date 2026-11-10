/* Feedback is persisted by the server before the UI reports success. */
(()=>{'use strict';const form=document.getElementById('feedbackForm');if(!form)return;
 const status=document.getElementById('feedbackStatus'),emailLink=document.getElementById('emailFeedback'),account=KicksAuth.current();let busy=false;
 if(account){form.elements.name.value=account.name;form.elements.email.value=account.email;}
 if(emailLink)emailLink.hidden=true;
 form.addEventListener('input',()=>{status.textContent='';});
 form.addEventListener('submit',async event=>{
  event.preventDefault();if(busy||!form.reportValidity())return;busy=true;const button=form.querySelector('[type=submit]');button.disabled=true;form.setAttribute('aria-busy','true');status.textContent='Sending feedback…';
  const body=new FormData(form);body.set('__RequestVerificationToken',JSON.parse(document.getElementById('kicks-auth-state').textContent).token);
  try{const response=await fetch('/feedback',{method:'POST',body,credentials:'same-origin'});if(!response.ok)throw Error(response.status===429?'Please wait a few minutes before sending more feedback.':'Check your feedback and try again.');const data=await response.json();status.textContent=data.message;form.reset();if(account){form.elements.name.value=account.name;form.elements.email.value=account.email;}}
  catch(error){status.textContent=error.message;}finally{busy=false;button.disabled=false;form.removeAttribute('aria-busy');}
 });
})();
