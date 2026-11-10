/* Public contact presentation comes from SQL; browser preferences never replace it. */
fetch('/api/site-presentation',{cache:'no-store'}).then(r=>{if(!r.ok)throw Error();return r.json();}).then(settings=>{
 if(!settings)return;
 for(const [id,value]of [['aboutCity',settings.city],['aboutContact',settings.contact]]){const target=document.getElementById(id);if(target){target.textContent=value||'';target.hidden=!value;}}
}).catch(()=>{});