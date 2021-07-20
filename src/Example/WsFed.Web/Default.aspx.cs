using Rotte.WsTrust;
using Rotte.WsTrust.RotteWS;
using System;
using System.Security.Claims;
using System.Threading;

namespace WsFed.Web
{
    public partial class Default : System.Web.UI.Page
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            LocalRepeater.DataSource = ((ClaimsIdentity)Thread.CurrentPrincipal.Identity).Claims;
            LocalRepeater.DataBind();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var timeout = TimeSpan.FromMinutes(5);
            var securityToken = WsFactory.GetIdentifyActAsToken();
            var rotteWS = WsFactory.GetRotteWS(securityToken, timeout);

            var claims = rotteWS.GetActAsClaims();
            RemoteRepeater.DataSource = claims;
            RemoteRepeater.DataBind();

            var request = new SearchRotteanmeldelseRequest();
            request.criteria = new RotteanmeldelseSearch
            {
                MaxRecords = 1
            };
            var response = rotteWS.SearchRotteanmeldelse(request);
        }
    }
}