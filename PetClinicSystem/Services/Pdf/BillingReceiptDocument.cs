using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PetClinicSystem.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PetClinicSystem.Services.Pdf
{
    public class BillingReceiptDocument : IDocument
    {
        public List<Billing> Items { get; }

        public BillingReceiptDocument(List<Billing> items)
        {
            Items = items ?? new List<Billing>();
        }

        public DocumentMetadata GetMetadata() => new DocumentMetadata();

        public void Compose(IDocumentContainer container)
        {
            var first = Items.FirstOrDefault();
            if (first == null)
                throw new Exception("Receipt contains no items.");

            decimal total = Items.Sum(x => x.Total);
            string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logo.jpg");

            container.Page(page =>
            {
                page.Margin(30);

                // ================= HEADER (CENTERED LOGO) ===================
                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Column(centered =>
                    {
                        if (File.Exists(logoPath))
                        {
                            centered.Item()
                                .AlignCenter()
                                .Height(70)
                                .Image(logoPath);
                        }

                        centered.Item()
                            .AlignCenter()
                            .Text("St. Paul Pet Clinic")
                            .FontSize(20)
                            .Bold();

                        centered.Item()
                            .AlignCenter()
                            .Text("Veterinary Care • Pharmacy • Laboratory")
                            .FontSize(10);
                    });

                    col.Item().PaddingVertical(10)
                        .BorderBottom(1)
                        .BorderColor(Colors.Grey.Lighten2);
                });

                // ================= CONTENT ===================
                page.Content().Column(col =>
                {
                    col.Item().PaddingBottom(10).Grid(grid =>
                    {
                        grid.Columns(2);

                        grid.Item().Column(left =>
                        {
                            left.Item().Text("Pet: " + first.Pet.Name).Bold();
                            left.Item().Text("Owner: " + first.Pet.Owner.FullName);
                            left.Item().Text("Transaction ID: " + first.TransactionId);
                        });

                        grid.Item().Column(right =>
                        {
                            right.Item().AlignRight().Text("Staff: " + first.Staff.FullName);
                            right.Item().AlignRight().Text("Date: " + first.BillingDate.ToString("MMM dd yyyy • hh:mm tt"));
                        });
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.ConstantColumn(40);
                            cols.ConstantColumn(70);
                            cols.ConstantColumn(70);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Service").Bold();
                            header.Cell().AlignCenter().Text("Qty").Bold();
                            header.Cell().AlignRight().Text("Price").Bold();
                            header.Cell().AlignRight().Text("Total").Bold();
                        });

                        foreach (var item in Items)
                        {
                            table.Cell().Text(item.ServiceName);
                            table.Cell().AlignCenter().Text(item.Quantity.ToString());
                            table.Cell().AlignRight().Text("₱" + item.ServicePrice.ToString("N2"));
                            table.Cell().AlignRight().Text("₱" + item.Total.ToString("N2"));
                        }
                    });

                    col.Item().PaddingTop(10)
                        .AlignRight()
                        .Text("Grand Total: ₱" + total.ToString("N2"))
                        .FontSize(14)
                        .Bold();

                    col.Item().PaddingTop(10)
                        .AlignCenter()
                        .Text("Thank you for trusting St. Paul Pet Clinic.")
                        .FontSize(10);

                    col.Item().PaddingTop(25).Grid(grid =>
                    {
                        grid.Columns(2);

                        grid.Item().Column(c =>
                        {
                            c.Item().BorderBottom(1).Height(20);
                            c.Item().AlignCenter().Text("Owner's Signature").FontSize(10);
                        });

                        grid.Item().Column(c =>
                        {
                            c.Item().BorderBottom(1).Height(20);
                            c.Item().AlignCenter().Text("Veterinary Staff").FontSize(10);
                        });
                    });
                });

                page.Footer()
                    .AlignCenter()
                    .Text("Generated on " + DateTime.Now.ToString("MMM dd yyyy • hh:mm tt"))
                    .FontSize(9);
            });
        }
    }
}
