﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RealArtists.ShipHub.Mail.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    
    #line 2 "..\..\Views\PurchaseOrganizationPlain.cshtml"
    using RealArtists.ShipHub.Mail;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Views\PurchaseOrganizationPlain.cshtml"
    using RealArtists.ShipHub.Mail.Models;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    public partial class PurchaseOrganizationPlain : ShipHubTemplateBase<PurchaseOrganizationMailMessage>
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");





            
            #line 5 "..\..\Views\PurchaseOrganizationPlain.cshtml"
  
  Layout = new RealArtists.ShipHub.Mail.Views.LayoutPlain() { Model = Model };


            
            #line default
            #line hidden
WriteLiteral("Thanks for purchasing a subscription to Ship - we hope you enjoy using it!\r\n\r\nDow" +
"nload a PDF receipt for your records:\r\n");


            
            #line 11 "..\..\Views\PurchaseOrganizationPlain.cshtml"
Write(Model.InvoicePdfUrl);

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n# How to manage your account:\r\n\r\nIf you need to change billing or payment inf" +
"o, or need to cancel your account, you can do so from within the Ship applicatio" +
"n. From the \"Ship\" menu, choose \"Manage Subscription\". Then click \"Manage\" for y" +
"our account.\r\n");


        }
    }
}
#pragma warning restore 1591
