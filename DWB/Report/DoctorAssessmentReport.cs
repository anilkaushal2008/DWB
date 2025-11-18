using DWB.APIModel;
using DWB.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.Xml;
using static System.Runtime.InteropServices.JavaScript.JSType;
using DWB.Services;

namespace DWB.Report
{
    public class DoctorAssessmentReport : IDocument
    {
        private readonly TblNsassessment nursing;
        private readonly TblDoctorAssessment doctor;
        private readonly List<TblDoctorAssmntMedicine> medicines;
        private readonly List<TblDoctorAssmntLab> labs;
        private readonly List<TblDoctorAssmntRadiology> radiologies;
        private readonly List<TblDoctorAssmntProcedure> procedures;
        private readonly TblUsers users;
        private readonly IndusCompanies companies;
        private readonly string _doctorSchedule;

        public DoctorAssessmentReport(
            TblNsassessment nursing,
            TblDoctorAssessment doctor,
            List<TblDoctorAssmntMedicine> medicines,
            List<TblDoctorAssmntLab> labs,
            List<TblDoctorAssmntRadiology> radiologies,
            List<TblDoctorAssmntProcedure> procedures,
            TblUsers users,
            IndusCompanies companies,
            string doctorSchedule = "" // 2. Add this parameter (default to empty)
            )
        {
            this.nursing = nursing;
            this.doctor = doctor;
            this.medicines = medicines ?? new List<TblDoctorAssmntMedicine>();
            this.labs = labs ?? new List<TblDoctorAssmntLab>();
            this.radiologies = radiologies ?? new List<TblDoctorAssmntRadiology>();
            this.procedures = procedures ?? new List<TblDoctorAssmntProcedure>();
            this.users = users;
            this.companies = companies;
            // 3. Assign the parameter to the field
            _doctorSchedule = doctorSchedule;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

       

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(10);

                // Default text style
                page.DefaultTextStyle(x => x.FontSize(8));

                // ===== HEADER =====
                page.Header().PaddingBottom(2).Column(headerCol =>
                {
                    headerCol.Item().Row(row =>
                    {
                        // Left: Logo
                        row.ConstantColumn(60).AlignLeft().Height(50).Element(img =>
                        {
                            img.Image("wwwroot/assets/images/INDUS-BW.jpg", ImageScaling.FitHeight);
                        });

                        // Center: Hospital Name + Address
                        row.RelativeColumn().Column(col =>
                        {
                            col.Item().Text(companies.Descript)
                                .FontSize(14).Bold().FontColor(Colors.Black).AlignCenter();

                            col.Item().Text(companies.Add1)
                                .FontSize(8).FontColor(Colors.Black).AlignCenter();

                            col.Item().Text(companies.Add2)
                                .FontSize(8).FontColor(Colors.Black).AlignCenter();
                        });

                        // Right: Contact
                        row.ConstantColumn(60).AlignRight().Height(50).Element(img =>
                        {
                            img.Image("wwwroot/assets/images/NABH-BW.jpeg", ImageScaling.FitHeight);
                        });
                    });

                    // Divider line
                    //headerCol.Item()
                    //    .PaddingVertical(4)
                    //    .LineHorizontal(1)
                    //    .LineColor(Colors.Black);
                });

                // ===== CONTENT =====
                page.Content().PaddingTop(0).Column(col =>
                {
                    // Main Title
                    //col.Item().PaddingBottom(2).AlignCenter().Text("DOCTOR ASSESSMENT REPORT")
                        //.FontSize(12).Bold().FontColor(Colors.Black);

                    //Patient details
                    col.Item()
                    .Background(Colors.White)
                    .Padding(0) // no outer border
                    .Table(table =>
    {
        // === Define columns once ===
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn();   // col 1
            columns.RelativeColumn(2); // col 2
            columns.RelativeColumn();   // col 3
        });

        // === Row 1 ===
        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(text =>
        {
            text.Span("UHID: ").Bold().FontSize(8);
            text.Span(nursing?.VchUhidNo ?? "N/A").FontSize(8);
        });

        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(text =>
        {
            text.Span("NAME: ").Bold().FontSize(8);
            text.Span(nursing?.VchHmsname?.ToUpper() ?? "N/A").FontSize(8);
        });

        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(text =>
        {
            text.Span("AGE / GENDER: ").Bold().FontSize(8);
            text.Span($"{(nursing?.VchHmsage ?? "0")} / {nursing?.VchGender?.ToUpper() ?? "N/A"}").FontSize(8);
        });

        // === Row 2 ===
        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(text =>
        {
            text.Span("DATE TIME: ").Bold().FontSize(8);
            text.Span(nursing?.DtCreated?.ToString("dd/MM/yyyy hh:mm tt") ?? "N/A").FontSize(8);
        });

        // CONSULTANT spans 2 columns
        table.Cell().ColumnSpan(1).Border(1).BorderColor(Colors.Black).Padding(4).Text(text =>
        {
            text.Span("CONSULTANT: ").Bold().FontSize(8);
            text.Span($"{(nursing?.VchHmsconsultant ?? "N/A").ToUpper()}").FontSize(8);
        });
        table.Cell().ColumnSpan(1).Border(1).BorderColor(Colors.Black).Padding(4).Text(text =>
        {
            text.Span("TIMING: ").Bold().FontSize(7);
            text.Span($"{(_doctorSchedule ?? " ").ToUpper()}").FontSize(6);
        });
        // 3. TIMING (Column 3) - This fills the 3rd column
        //table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(text =>
        //{
        //    // Only show the label and time if the string is not empty
        //    if (!string.IsNullOrWhiteSpace(_doctorSchedule))
        //    {
        //        // Optional: Add a label like "TIMING:" if you want
        //        // text.Span("TIMING: ").Bold().FontSize(8); 

        //        text.Span(_doctorSchedule).FontSize(8);
        //    }
        //    else
        //    {
        //        // If null/empty, print nothing, but keep the cell borders
        //        text.Span("");
        //    }
        //});
    });                   
                    col.Item().PaddingVertical(4);


                    //Nursing assessment
                    col.Item().Padding(0).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        // ===== Title Row =====
                        table.Cell().ColumnSpan(8)
                            .Background(Colors.Grey.Lighten3)
                            .Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text("NURSING ASSESSMENT").Bold().FontSize(9).AlignCenter();

                        // ===== Row 1: Vitals (8 cells) =====
                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text(t =>
                            {
                                t.Span("BP: ").Bold().FontSize(8);
                                t.Span(nursing?.VchBloodPressure ?? "N/A").FontSize(8);
                            });

                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text(t =>
                            {
                                t.Span("SPO2: ").Bold().FontSize(8);
                                t.Span($"{nursing?.DecSpO2 ?? 0}").FontSize(8);
                            });

                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text(t =>
                            {
                                t.Span("PULSE: ").Bold().FontSize(8);
                                t.Span(nursing?.VchPulse ?? "N/A").FontSize(8);
                            });

                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text(t =>
                            {
                                t.Span("HEIGHT: ").Bold().FontSize(8);
                                t.Span($"{nursing?.DecHeight ?? 0}").FontSize(8);
                            });

                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text(t =>
                            {
                                t.Span("TEMP: ").Bold().FontSize(8);
                                t.Span($"{nursing?.DecTemperature ?? "N/A"}").FontSize(8);
                            });

                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text(t =>
                            {
                                t.Span("R RATE: ").Bold().FontSize(8);
                                t.Span($"{nursing?.DecRespiratoryRate ?? 0}").FontSize(8);
                            });

                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text(t =>
                            {
                                t.Span("WEIGHT: ").Bold().FontSize(8);
                                t.Span($"{nursing?.DecWeight ?? 0}").FontSize(8);
                            });

                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text(t =>
                            {
                                t.Span("ALCOHOL: ").Bold().FontSize(8);
                                t.Span(nursing?.BitIsAlcoholic == true ? "YES" : "NO").FontSize(8);
                            });

                        col.Item().Table(t =>
                        {
                            // Define 3 columns: two equal width for Smoking & Fall Risk, last one takes remaining space for Allergy
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1); // Smoking
                                c.RelativeColumn(1); // Fall Risk
                                c.RelativeColumn(6); // Allergy (remaining width)
                            });

                            // Smoking
                            t.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(txt =>
                            {
                                txt.Span("SMOKING: ").Bold().FontSize(8);
                                txt.Span(nursing?.BitIsSmoking == true ? "YES" : "NO").FontSize(8);
                            });

                            // Fall Risk
                            t.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(txt =>
                            {
                                txt.Span("FALL RISK: ").Bold().FontSize(8);
                                txt.Span(nursing?.BitFallRisk == true ? "YES" : "NO").FontSize(8);
                            });

                            // Allergy
                            string allergyText = (nursing?.BitIsAllergical ?? false)
                                ? $"YES ({(string.IsNullOrWhiteSpace(nursing?.VchAllergicalDrugs) ? "N/A" : nursing.VchAllergicalDrugs.ToUpper())})"
                                : "NO";

                            t.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(txt =>
                            {
                                txt.Span("ALLERGY: ").Bold().FontSize(8);
                                txt.Span(allergyText).FontSize(8);
                            });
                        });
                    });
                    col.Item().PaddingVertical(4);
                    //end nursing assessment table

                    // ===== Doctor Assessment =====
                    col.Item().Padding(0).Table(table =>
                    {
                        // Single column spanning full width
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                        });

                        // ===== Title row =====
                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                            .Background(Colors.Grey.Lighten3)
                            .Text("DOCTOR ASSESSMENT".ToUpper())
                            .Bold().FontSize(9).AlignCenter();

                        // ===== Row 1: Complaints =====
                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(txt =>
                        {
                            txt.Span("COMPLAINTS: ").Bold().FontSize(8);
                            txt.Span($"{doctor?.VchChiefcomplaints?.ToUpper() ?? "N/A"}").FontSize(8);
                        });

                        // ===== Row 2: Diagnosis =====
                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(txt =>
                        {
                            txt.Span("MEDICAL HISTORY: ").Bold().FontSize(8);
                            txt.Span($"{doctor?.VchMedicalHistory?.ToUpper() ?? "N/A"}").FontSize(8);
                        });

                        // ===== Row 3: Remarks =====
                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(txt =>
                        {
                            txt.Span("DIAGNOSIS: ").Bold().FontSize(8);
                            txt.Span($"{doctor?.VchDiagnosis?.ToUpper() ?? "N/A"}").FontSize(8);
                        });

                        // ===== Row 4: Prescription =====
                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(txt =>
                        {
                            txt.Span("REMARKS: ").Bold().FontSize(8);
                            txt.Span($"{doctor?.VchRemarks?.ToUpper() ?? "N/A"}").FontSize(8);
                        });
                    });
                    col.Item().PaddingVertical(4);


                    //===== Labs, Radiology & Procedures Combined Card =====
                    if (labs.Any() || radiologies.Any() || procedures.Any())
                    {
                        col.Item().PaddingTop(0).Background(Colors.White)
                            .Border(1).BorderColor(Colors.Black)
                            .Padding(5).Column(card =>
                            {
                                // Labs
                                if (labs.Any())
                                {
                                    card.Item().Text("LAB TESTS").Bold().FontSize(9);

                                    string labTestsLine = "• " + string.Join(", ", labs.Select(l => (l.VchTestName ?? "").ToUpper()));

                                    // Display all lab tests in one line
                                    card.Item().Text(labTestsLine).FontSize(8);

                                    card.Item().PaddingBottom(4); // space before next section
                                }

                                // Radiology
                                if (radiologies.Any())
                                {
                                    card.Item().Text("RADIOLOGY").Bold().FontSize(9);
                                    string allradiology = "• " + string.Join(", ", radiologies.Select(r => (r.VchRadiologyName ?? "").ToUpper()));

                                    // Display all radiology in one line
                                    card.Item().Text(allradiology).FontSize(8);

                                    card.Item().PaddingBottom(4);
                                }

                                // Procedures
                                if (procedures.Any())
                                {
                                    card.Item().Text("PROCEDURES").Bold().FontSize(9);
                                    string allprocedure = "• " + string.Join(", ", procedures.Select(p => (p.VchProcedureName ?? "").ToUpper()));

                                    // Display all procedures in one line
                                    card.Item().Text(allprocedure).FontSize(8);
                                }
                            });
                    }
                    col.Item().PaddingVertical(4);
                    // ===== Medicines Card =====
                    if (medicines.Any())
                    {
                        col.Item().Background(Colors.White)
                            .Border(1).BorderColor(Colors.Black)
                            .Padding(8).Column(docCard =>
                            {
                                // === Title with note ===
                                docCard.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("PRESCRIBED MEDICINES")
                                        .Bold().FontSize(9).FontColor(Colors.Black);

                                    row.AutoItem().AlignRight()
                                        .Text("Timing*: BBF = Before Breakfast, ABF = After Breakfast, BL = Before Lunch, AL = After Lunch, BD = Before Dinner, AD = After Dinner")
                                        .SemiBold().FontSize(7).FontColor(Colors.Black);
                                });

                                docCard.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(20); // Sr. No.
                                        columns.RelativeColumn(3); // Medicine
                                        columns.RelativeColumn(1); // Quantity
                                        columns.RelativeColumn(1); // Frequency
                                        columns.RelativeColumn(1); // Duration
                                                                   // Smaller width for timing columns
                                        columns.ConstantColumn(20); // BBF
                                        columns.ConstantColumn(20); // ABF
                                        columns.ConstantColumn(20); // BL
                                        columns.ConstantColumn(20); // AL
                                        columns.ConstantColumn(20); // BD
                                        columns.ConstantColumn(20); // AD
                                    });

                                    // ===== Header =====
                                    table.Header(header =>
                                    {
                                        // First Row - main header with * mark
                                        header.Cell().RowSpan(3).Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text("Sr. No.").Bold().FontSize(7);
                                        header.Cell().RowSpan(3).Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text("MEDICINE").Bold().FontSize(7);
                                        header.Cell().RowSpan(3).Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text("QUANTITY").Bold().FontSize(7);
                                        header.Cell().RowSpan(3).Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text("FREQUENCY").Bold().FontSize(7);
                                        header.Cell().RowSpan(3).Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text("DURATION").Bold().FontSize(7);

                                        header.Cell().ColumnSpan(6).Border(1).BorderColor(Colors.Black).Padding(3)
                                            .AlignCenter().Text("TIMING*").Bold().FontSize(7);

                                        // Second Row - Morning/Afternoon/Night with * mark
                                        header.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black).Padding(3)
                                            .AlignCenter().Text("Morning (Breakfast)").Bold().FontSize(7);
                                        header.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black).Padding(3)
                                            .AlignCenter().Text("Afternoon (Lunch)").Bold().FontSize(7);
                                        header.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black).Padding(3)
                                            .AlignCenter().Text("Night (Dinner)").Bold().FontSize(7);

                                        // Third Row - actual small columns
                                        header.Cell().Border(1).BorderColor(Colors.Black).Padding(3).Text("BBF").Bold().FontSize(7);
                                        header.Cell().Border(1).BorderColor(Colors.Black).Padding(3).Text("ABF").Bold().FontSize(7);
                                        header.Cell().Border(1).BorderColor(Colors.Black).Padding(3).Text("BL").Bold().FontSize(7);
                                        header.Cell().Border(1).BorderColor(Colors.Black).Padding(3).Text("AL").Bold().FontSize(7);
                                        header.Cell().Border(1).BorderColor(Colors.Black).Padding(3).Text("BD").Bold().FontSize(7);
                                        header.Cell().Border(1).BorderColor(Colors.Black).Padding(3).Text("AD").Bold().FontSize(7);
                                    });

                                    // ===== Medicine Rows =====
                                    int sr = 1;
                                    foreach (var med in medicines)
                                    {
                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text(sr++.ToString()).FontSize(7);
                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text((med.VchMedicineName ?? "").ToUpper()).FontSize(7);
                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text((med.IntQuantity)).FontSize(7);
                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text((med.VchFrequency ?? "").ToUpper()).FontSize(7);
                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text((med.VchDuration ?? "").ToUpper()).FontSize(7);

                                        // Timing ticks
                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text(med.BitBbf ? "✔" : "").FontSize(9);
                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text(med.BitAbf ? "✔" : "").FontSize(9);
                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text(med.BitBl ? "✔" : "").FontSize(9);
                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text(med.BitAl ? "✔" : "").FontSize(9);
                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text(med.BitBd ? "✔" : "").FontSize(9);
                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text(med.BitAd ? "✔" : "").FontSize(9);
                                    }
                                });
                            });
                    }

                    // ===== Nutritional Consultation + Follow Up Card =====
                    col.Item().PaddingTop(6).Background(Colors.White)
                        .Border(1).BorderColor(Colors.Black)
                        .Padding(5).Row(row =>
                        {
                            // Nutritional Consultation
                            row.RelativeColumn().Text(text =>
                            {
                                text.Span("NUTRITIONAL CONSULTATION: ").Bold().FontSize(9);
                                text.Span(doctor?.BitNutritionalConsult == true ? "• YES" : "• NO").FontSize(8);
                            });

                            // Follow Up Date
                            row.RelativeColumn().AlignRight().Text(text =>
                            {
                                text.Span("FOLLOW UP DATE: ").Bold().FontSize(9);
                                text.Span(doctor?.DtFollowUpDate.HasValue == true
                                    ? doctor.DtFollowUpDate.Value.ToString("• " + "dd/MM/yyyy")
                                    : "• NA").FontSize(8);
                            });
                        });


                    // ===== Explanation + Signature (right-aligned) =====
                    col.Item().PaddingTop(6)
                        .Background(Colors.White)
                        .Border(1).BorderColor(Colors.Black)
                        .Padding(2).Column(card =>
                        {
                            // Explanation text
                            card.Item().PaddingBottom(0).Text("I have explained to the patient the disease process, proposed plan of care, and possible side effects in their own language.")
                            .Italic().FontSize(9);


                            // Row where signature sits at the right edge
                            card.Item().Row(row =>
                            {
                                row.RelativeColumn(); // consumes available space, pushing signature to the right

                                // Fixed-width column for signature to avoid layout conflicts
                                row.ConstantColumn(110).Column(sig =>
                                {
                                    // Signature image area (fixed height)
                                    sig.Item().PaddingTop(0).Height(45).Element(cell =>
                                    {
                                        if (!string.IsNullOrWhiteSpace(users?.VchSignFileName))
                                        {
                                            // align image to the right inside the box and scale to fit the area
                                            cell.AlignRight().Image("wwwroot/Uploads/Signature/" + users.VchSignFileName, ImageScaling.FitArea);
                                        }
                                        else
                                        {
                                            // Placeholder box centered inside the fixed area
                                            cell.Border(1).BorderColor(Colors.Red.Medium)
                                            .AlignCenter().AlignMiddle()
                                            .Text("Signature Missing")
                                            .FontSize(8).Italic().FontColor(Colors.Red.Medium);
                                        }
                                    });
                                    // Consultant's name aligned right under the signature box
                                    sig.Item().AlignRight().Text(
                                    string.IsNullOrWhiteSpace(nursing?.VchHmsconsultant) ? "N/A" : nursing.VchHmsconsultant.ToUpper()).FontSize(8).Bold();
                                });
                            });
                        });


                    // ===== Important Notes Card =====
                    col.Item().PaddingTop(10).AlignCenter().Element(card =>
                    {
                        card.Border(1)
                            .BorderColor(Colors.Black)
                            .Background(Colors.White)
                            .Padding(7)
                            .Width(350) // fixed width
                            .Column(note =>
                            {
                                note.Item().Text("IMPORTANT NOTES")
                                    .Bold()
                                    .FontSize(8)
                                    .FontColor(Colors.Red.Medium);

                                note.Item().PaddingTop(4)
                                    .Text("• Kindly confirm your appointment one day prior at 01762-512666")
                                    .FontSize(7);

                                note.Item().Text("• In case of emergency (Fever >101.5°F / 38.6°C, new/worsening pain, chest pain, breathlessness, vomiting, etc.), please contact 01762-512600 & Dr. Mayank Sharma (Reg No. 9273) at 01762-512600")
                                    .FontSize(7);
                            });
                    });
                });

                // ===== FOOTER ===== (must be outside content)
                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Print on: ").SemiBold().FontSize(7);
                    txt.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm")).FontSize(7);
                });             
            });
        }   
        
    }
}

