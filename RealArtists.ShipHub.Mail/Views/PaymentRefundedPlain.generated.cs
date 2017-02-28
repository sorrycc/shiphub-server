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
    
    #line 2 "..\..\Views\PaymentRefundedPlain.cshtml"
    using RealArtists.ShipHub.Mail;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Views\PaymentRefundedPlain.cshtml"
    using RealArtists.ShipHub.Mail.Models;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    public partial class PaymentRefundedPlain : ShipHubTemplateBase<PaymentRefundedMailMessage>
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");





            
            #line 5 "..\..\Views\PaymentRefundedPlain.cshtml"
  
  Layout = new RealArtists.ShipHub.Mail.Views.LayoutPlain() { Model = Model };


            
            #line default
            #line hidden
WriteLiteral("We\'ve issued a refund for ");


            
            #line 8 "..\..\Views\PaymentRefundedPlain.cshtml"
                     Write(string.Format("{0:C}", Model.AmountRefunded));

            
            #line default
            #line hidden
WriteLiteral(" to your ");


            
            #line 8 "..\..\Views\PaymentRefundedPlain.cshtml"
                                                                           Write(PaymentMethodSummaryPlain(Model.PaymentMethodSummary));

            
            #line default
            #line hidden
WriteLiteral(".\r\n\r\nDownload a PDF receipt for this transaction:\r\n");


            
            #line 11 "..\..\Views\PaymentRefundedPlain.cshtml"
Write(Model.CreditNotePdfUrl);

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\nThanks for your business!\r\n");


        }
    }
}
#pragma warning restore 1591
