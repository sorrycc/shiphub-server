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
    
    #line 2 "..\..\Views\PaymentSucceededPersonalPlain.cshtml"
    using RealArtists.ShipHub.Mail;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Views\PaymentSucceededPersonalPlain.cshtml"
    using RealArtists.ShipHub.Mail.Models;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    public partial class PaymentSucceededPersonalPlain : ShipHubTemplateBase<PaymentSucceededPersonalMailMessage>
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");





            
            #line 5 "..\..\Views\PaymentSucceededPersonalPlain.cshtml"
  
  Layout = new RealArtists.ShipHub.Mail.Views.LayoutPlain() { Model = Model };


            
            #line default
            #line hidden
WriteLiteral("We received payment for your personal Ship subscription.\r\n\r\n");


            
            #line 10 "..\..\Views\PaymentSucceededPersonalPlain.cshtml"
Write(string.Format("{0:C}", Model.AmountPaid));

            
            #line default
            #line hidden
WriteLiteral(" was charged to your card ending in ");


            
            #line 10 "..\..\Views\PaymentSucceededPersonalPlain.cshtml"
                                                                        Write(Model.LastCardDigits);

            
            #line default
            #line hidden
WriteLiteral(" and covers service through ");


            
            #line 10 "..\..\Views\PaymentSucceededPersonalPlain.cshtml"
                                                                                                                         Write(Model.ServiceThroughDate.ToString("MMM d, yyyy"));

            
            #line default
            #line hidden
WriteLiteral(".\r\n\r\nDownload a PDF receipt for your records:\r\n");


            
            #line 13 "..\..\Views\PaymentSucceededPersonalPlain.cshtml"
Write(Model.InvoicePdfUrl);

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\nWe appreciate your business!\r\n");


        }
    }
}
#pragma warning restore 1591
