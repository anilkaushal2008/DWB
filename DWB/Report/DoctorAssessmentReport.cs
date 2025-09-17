using DWB.APIModel;
using DWB.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.Xml;

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

        public DoctorAssessmentReport(
            TblNsassessment nursing,
            TblDoctorAssessment doctor,
            List<TblDoctorAssmntMedicine> medicines,
            List<TblDoctorAssmntLab> labs,
            List<TblDoctorAssmntRadiology> radiologies,
            List<TblDoctorAssmntProcedure> procedures,
            TblUsers users)
        {
            this.nursing = nursing;
            this.doctor = doctor;
            this.medicines = medicines ?? new List<TblDoctorAssmntMedicine>();
            this.labs = labs ?? new List<TblDoctorAssmntLab>();
            this.radiologies = radiologies ?? new List<TblDoctorAssmntRadiology>();
            this.procedures = procedures ?? new List<TblDoctorAssmntProcedure>();
            this.users = users;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        //public void Compose(IDocumentContainer container)
        //{
        //    container.Page(page =>
        //    {
        //        page.Size(PageSizes.A4);
        //        page.Margin(30);

        //        // Set default font size
        //        page.DefaultTextStyle(x => x.FontSize(9));

        //        // ===== HEADER =====
        //        page.Header().Row(row =>
        //        {
        //            // Left Logo
        //            row.ConstantColumn(80).Height(50).Image("wwwroot/assets/images/INBGWHITE.png");

        //            // Centered Hospital Name
        //            row.RelativeColumn().AlignCenter().Text("Indus International Hospital")
        //                .FontSize(17).Bold().FontColor(Colors.Black);

        //            // (Optional) Right side empty space to balance alignment
        //            row.ConstantColumn(80);
        //        });

        //        // ===== CONTENT =====
        //        page.Content().Column(col =>
        //        {
        //            // Title
        //            col.Item().AlignCenter().Text("DOCTOR ASSESSMENT REPORT")
        //                .FontSize(15).Bold().FontColor(Colors.Black);

        //            col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);



        //            // ===== Patient & Consultant Cards =====
        //            col.Item().Row(row =>
        //            {
        //                // Patient Details Card
        //                row.RelativeColumn().Background(Colors.White)
        //                    .Border(1).BorderColor(Colors.Grey.Lighten2)
        //                    .Padding(10).PaddingRight(5) // Padding to the right for spacing
        //                    .Column(patient =>
        //                    {
        //                        patient.Item().Text("PATIENT DETAILS").Bold().FontSize(10);
        //                        patient.Item().Text($"NAME : {nursing?.VchHmsname?.ToUpper() ?? "N/A"}");
        //                        patient.Item().Text($"AGE : {nursing?.IntAge ?? 0}  |  GENDER : {nursing?.VchGender?.ToUpper() ?? "N/A"}");
        //                        patient.Item().Text($"VISIT : {nursing?.IntIhmsvisit ?? 0} | DATE : {nursing?.DtCreated.Value.ToString("dd/MM/yyyy")}");
        //                        patient.Item().Text($"CONSULTANT : {nursing?.VchHmsconsultant?.ToUpper() ?? "N/A"}");
        //                    });

        //                // Nursing assessment Details Card
        //                row.RelativeColumn().Background(Colors.White)
        //                    .Border(1).BorderColor(Colors.Grey.Lighten2)
        //                    .Padding(10).PaddingLeft(5) // Margin to the left for spacing
        //                    .Column(nu =>
        //                    {
        //                        nu.Item().Text("NURSING ASSESSMENT").Bold().FontSize(10);
        //                        nu.Item().Text($"BP : {nursing?.VchBloodPressure ?? "N/A"} | SpO2 : {nursing?.DecSpO2 ?? 0}");
        //                        nu.Item().Text($"PULSE : {nursing?.VchPulse ?? "N/A"} | HEIGHT : {nursing?.DecHeight ?? 0}");
        //                        nu.Item().Text($"TEMP : {nursing?.DecTemperature ?? "N/A"} | RESPIRATORY RATE : {nursing?.DecRespiratoryRate ?? 0}");
        //                        nu.Item().Text(nursing?.BitIsAllergical == true ? $"ALLERGIES : YES | ALLERGY SOURCE : {(string.IsNullOrWhiteSpace(nursing?.VchAllergicalDrugs) ? "N/A" : nursing.VchAllergicalDrugs.ToUpper())}" : "ALLERGIES : NO");
        //                        nu.Item().Text($"ALCOHOL : {(nursing?.BitIsAlcoholic == true ? "YES" : "NO")} | " + $"SMOKING : {(nursing?.BitIsSmoking == true ? "YES" : "NO")}");
        //                        nu.Item().Text($"FALL RISK : {(nursing?.BitFallRisk == true ? "YES" : "NO")}");
        //                    });


        //                col.Item().PaddingVertical(8);
        //                // Doctor Assessment Header
        //                col.Item().Text("👨‍⚕️DOCTOR ASSESSMENT").FontSize(10).SemiBold();
        //                // ===== Doctor Assessment Details =====
        //                col.Item().Background(Colors.White).Padding(4).Column(doc =>
        //                        {
        //                            doc.Item().Text($"COMPLAINTS: {(doctor?.VchChiefcomplaints ?? "N/A").ToUpper()}");
        //                            doc.Item().Text($"DIAGNOSIS: {(doctor?.VchDiagnosis ?? "N/A").ToUpper()}");
        //                            doc.Item().Text($"REMARKS: {(doctor?.VchRemarks ?? "N/A").ToUpper()}");
        //                        });

        //                col.Item().PaddingVertical(8);

        //                // ===== Medicines Table =====
        //                if (medicines.Any())
        //                {
        //                    col.Item().Text("💊PRESCRIBED MEDICINES").FontSize(10).SemiBold();

        //                    col.Item().Table(table =>
        //                            {
        //                                table.ColumnsDefinition(columns =>
        //                                {
        //                                    columns.RelativeColumn(2); // Medicine
        //                                    columns.RelativeColumn(1); // Dosage
        //                                    columns.RelativeColumn(1); // Frequency
        //                                    columns.RelativeColumn(1); // Duration
        //                                    columns.RelativeColumn(3); // Timing
        //                                });

        //                                table.Header(header =>
        //                                {
        //                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("MEDICINE").SemiBold();
        //                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("DOSAGE").SemiBold();
        //                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("FREQUENCY").SemiBold();
        //                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("DURATION").SemiBold();
        //                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("TIMING").SemiBold();
        //                                });

        //                                foreach (var med in medicines)
        //                                {
        //                                    table.Cell().Padding(4).Text((med.VchMedicineName ?? "").ToUpper());
        //                                    table.Cell().Padding(4).Text((med.VchDosage ?? "").ToUpper());
        //                                    table.Cell().Padding(4).Text((med.VchFrequency ?? "").ToUpper());
        //                                    table.Cell().Padding(4).Text((med.VchDuration ?? "").ToUpper());

        //                                    // Build Timing String
        //                                    var timings = new List<string>();
        //                                    if (med.BitBbf == true) timings.Add("BBF");
        //                                    if (med.BitAbf == true) timings.Add("ABF");
        //                                    if (med.BitBl == true) timings.Add("BL");
        //                                    if (med.BitAl == true) timings.Add("AL");
        //                                    if (med.BitBd == true) timings.Add("BD");
        //                                    if (med.BitAd == true) timings.Add("AD");

        //                                    var timingText = timings.Count > 0 ? string.Join(", ", timings) : "N/A";

        //                                    // ✅ Add Timing to the table
        //                                    table.Cell().Padding(4).Text(timingText);
        //                                }
        //                                // ✅ Add Abbreviation Legend at the bottom
        //                                col.Item().PaddingTop(4).Text("Timing Abbreviations:").Bold();
        //                                col.Item().Text("BBF = Before Breakfast, ABF = After Breakfast, BL = Before Lunch, AL = After Lunch, BD = Before Dinner, AD = After Dinner")
        //                                    .FontSize(8).FontColor(Colors.Black);
        //                            });

        //                }

        //                col.Item().PaddingVertical(8);

        //                // ===== Lab Tests =====
        //                if (labs.Any())
        //                {
        //                    col.Item().Text("🧪LAB TESTS").FontSize(10).SemiBold();
        //                    foreach (var lab in labs)
        //                        col.Item().Text($"• {(lab.VchTestName ?? "").ToUpper()}");
        //                }

        //                col.Item().PaddingVertical(8);

        //                // ===== Radiology =====
        //                if (radiologies.Any())
        //                {
        //                    col.Item().Text("🩻RADIOLOGY").FontSize(10).SemiBold();
        //                    foreach (var r in radiologies)
        //                        col.Item().Text($"• {(r.VchRadiologyName ?? "").ToUpper()}");
        //                }

        //                col.Item().PaddingVertical(8);

        //                // ===== Procedures =====
        //                if (procedures.Any())
        //                {
        //                    col.Item().Text("PROCEDURES").FontSize(10).SemiBold();
        //                    foreach (var p in procedures)
        //                        col.Item().Text($"• {(p.VchProcedureName ?? "").ToUpper()}");
        //                }
        //                // ===== Nutritional Consultation =====
        //                col.Item().PaddingVertical(8);

        //                // ===== NUTRITIONAL CONSULTATION (Row with 2 Cards) =====
        //                col.Item().PaddingTop(4).Row(row =>
        //                {
        //                    // Card 1: Nutritional Consultation Yes/No
        //                    row.RelativeColumn().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(card =>
        //                    {
        //                        card.Item().Text("🍎NUTRITIONAL CONSULTATION").FontSize(8).SemiBold();
        //                        card.Item().Text(doctor?.BitNutritionalConsult == true ? "• YES" : "• NO");
        //                    });

        //                    // Card 2: Follow-up Date
        //                    row.RelativeColumn().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(card =>
        //                    {
        //                        card.Item().Text("FOLLOW UP DATE").FontSize(8).SemiBold();
        //                        card.Item().Text(doctor?.DtFollowUpDate.HasValue == true
        //                            ? doctor.DtFollowUpDate.Value.ToString("• dd/MM/yyyy")
        //                            : "• NA");
        //                    });
        //                });

        //                // ===== Doctor Explanation + Signature Card =====                       
        //                col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(card =>
        //                {
        //                    // Explanation
        //                    card.Item().Text("I have explained to patient the disease process, proposed plan of care and possible side effects in their own language.")
        //                        .Italic();

        //                    card.Item().PaddingVertical(4);

        //                    // Signature + Name on the right
        //                    card.Item().AlignCenter().Row(row =>
        //                    {
        //                        row.RelativeColumn(); // Left side empty (takes remaining space)

        //                        // Right side: Signature + Consultant Name
        //                        row.ConstantColumn(100).Column(sig =>
        //                        {
        //                            // Signature Image
        //                            sig.Item().Height(50).Element(cell =>
        //                            {
        //                                if (!string.IsNullOrWhiteSpace(users?.VchSignFileName))
        //                                {
        //                                    cell.Image("wwwroot/Uploads/Signature/" + users.VchSignFileName, ImageScaling.FitWidth);
        //                                }
        //                                else
        //                                {
        //                                    cell.Text("Signature missing")
        //                                        .FontSize(9)
        //                                        .Italic()
        //                                        .FontColor(Colors.Red.Medium)
        //                                        .AlignCenter();
        //                                }
        //                            });

        //                            // Consultant Name below signature
        //                            sig.Item().AlignCenter().Text(
        //                                string.IsNullOrWhiteSpace(nursing?.VchHmsconsultant)
        //                                    ? "N/A"
        //                                    : nursing.VchHmsconsultant.ToUpper()
        //                            )
        //                            .FontSize(9)
        //                            .Bold();
        //                        });
        //                    });
        //                });

        //                //col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(card =>
        //                //{
        //                //    // Explanation
        //                //    card.Item().Text("I have explained to patient the disease process, proposed plan of care and possible side effects in their own language.")
        //                //        .Italic();

        //                //    card.Item().PaddingVertical(4);

        //                //    // Signature on top
        //                //    card.Item().Width(80).Height(50).Element(cell =>
        //                //    {
        //                //        if (!string.IsNullOrWhiteSpace(users?.VchSignFileName))
        //                //        {
        //                //            cell.Image("wwwroot/Uploads/Signature/" + users.VchSignFileName, ImageScaling.FitWidth);
        //                //        }
        //                //        else
        //                //        {
        //                //            cell.Text("Signature missing")
        //                //                .FontSize(9)
        //                //                .Italic()
        //                //                .FontColor(Colors.Red.Medium)
        //                //                .AlignCenter();
        //                //        }
        //                //    });

        //                //    // Consultant Name below signature
        //                //    card.Item().AlignLeft().Text(
        //                //        string.IsNullOrWhiteSpace(nursing?.VchHmsconsultant)
        //                //            ? "N/A"
        //                //            : nursing.VchHmsconsultant.ToUpper()
        //                //    )
        //                //    .FontSize(9)
        //                //    .Bold();
        //                //});


        //                // Important Notes Box
        //                col.Item().PaddingTop(4).Background(Colors.Grey.Lighten4).Padding(8).Column(note =>
        //                        {
        //                            note.Item().Text("NOTE:").Bold();
        //                            note.Item().Text("• Kindly confirm your appointment one day prior at 01762-512666");
        //                            note.Item().Text("• In case of emergency (Fever of >101.5°F / 38.6°C, new pain, worsening previous pain, chest pain, breathlessness, vomiting etc.), please contact at Ph-01762-512600 & Dr. Mayank Sharma (Reg No. 9273) at Ph-01762-512600");


        //                            // ===== FOOTER =====
        //                            page.Footer().AlignCenter().Text(txt =>
        //                                    {
        //                                        txt.Span("Generated on: ").SemiBold();
        //                                        txt.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm"));
        //                                    });
        //                        });
        //            });
        //        });

        //    });
        //}

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(10);

                // Default small font
                page.DefaultTextStyle(x => x.FontSize(8));
              
                // ===== HEADER =====
                page.Header().PaddingBottom(2).Row(row =>
                {
                    //Left Logo
                    row.ConstantColumn(80).Height(40).Image("wwwroot/assets/images/INBGWHITE.png");

                    //Centered Hospital Name
                    row.RelativeColumn().AlignCenter().Text("Indus International Hospital")
                        .FontSize(14).Bold().FontColor(Colors.Black);
                          // 🔽 reduce space below

                    //Right Empty
                    row.ConstantColumn(80);
                });

                // ===== CONTENT =====
                page.Content().PaddingTop(0).Column(col =>
                {
                    // Main Title
                    col.Item().AlignCenter().Text("DOCTOR ASSESSMENT REPORT")
                     .FontSize(12).Bold().FontColor(Colors.Black);   // 🔽 no extra padding on top

                    //col.Item().PaddingVertical(4).LineHorizontal(0.8).LineColor(Colors.Grey.Lighten2);

                    // ===== Patient Details Card =====
                    col.Item().Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(patient =>
                    {
                        patient.Item().Text("PATIENT DETAILS").Bold().FontSize(9);
                        patient.Item().Text($"NAME : {nursing?.VchHmsname?.ToUpper() ?? "N/A"}");
                        patient.Item().Text($"AGE : {nursing?.IntAge ?? 0}  |  GENDER : {nursing?.VchGender?.ToUpper() ?? "N/A"}");
                        patient.Item().Text($"VISIT : {nursing?.IntIhmsvisit ?? 0} | DATE : {nursing?.DtCreated.Value.ToString("dd/MM/yyyy")}");
                        patient.Item().Text($"CONSULTANT : {nursing?.VchHmsconsultant?.ToUpper() ?? "N/A"}");
                    });

                    col.Item().PaddingVertical(8);

                    // ===== Nursing & Doctor Assessment Card =====
                    col.Item().Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(docCard =>
                    {
                        // Nursing Assessment
                        docCard.Item().Text("NURSING ASSESSMENT").Bold().FontSize(9);
                        docCard.Item().Text($"BP : {nursing?.VchBloodPressure ?? "N/A"} | SpO2 : {nursing?.DecSpO2 ?? 0}");
                        docCard.Item().Text($"PULSE : {nursing?.VchPulse ?? "N/A"} | HEIGHT : {nursing?.DecHeight ?? 0}");
                        docCard.Item().Text($"TEMP : {nursing?.DecTemperature ?? "N/A"} | RESPIRATORY RATE : {nursing?.DecRespiratoryRate ?? 0}");
                        docCard.Item().Text(nursing?.BitIsAllergical == true
                            ? $"ALLERGIES : YES | ALLERGY SOURCE : {(string.IsNullOrWhiteSpace(nursing?.VchAllergicalDrugs) ? "N/A" : nursing.VchAllergicalDrugs.ToUpper())}"
                            : "ALLERGIES : NO");
                        docCard.Item().Text($"ALCOHOL : {(nursing?.BitIsAlcoholic == true ? "YES" : "NO")} | " +
                                           $"SMOKING : {(nursing?.BitIsSmoking == true ? "YES" : "NO")}");
                        docCard.Item().Text($"FALL RISK : {(nursing?.BitFallRisk == true ? "YES" : "NO")}");

                        docCard.Item().PaddingVertical(6);

                        // Doctor Assessment
                        docCard.Item().Text("DOCTOR ASSESSMENT").Bold().FontSize(9);
                        docCard.Item().Text($"COMPLAINTS: {(doctor?.VchChiefcomplaints ?? "N/A").ToUpper()}");
                        docCard.Item().Text($"DIAGNOSIS: {(doctor?.VchDiagnosis ?? "N/A").ToUpper()}");
                        docCard.Item().Text($"REMARKS: {(doctor?.VchRemarks ?? "N/A").ToUpper()}");

                        docCard.Item().PaddingVertical(6);

                        // Medicines
                        if (medicines.Any())
                        {
                            docCard.Item().Text("PRESCRIBED MEDICINES").Bold().FontSize(9);

                            docCard.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(4);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(3);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("MEDICINE").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("DOSAGE").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("FREQUENCY").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("DURATION").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("TIMING").SemiBold();
                                });

                                foreach (var med in medicines)
                                {
                                    table.Cell().Padding(3).Text((med.VchMedicineName ?? "").ToUpper());
                                    table.Cell().Padding(3).Text((med.VchDosage ?? "").ToUpper());
                                    table.Cell().Padding(3).Text((med.VchFrequency ?? "").ToUpper());
                                    table.Cell().Padding(3).Text((med.VchDuration ?? "").ToUpper());

                                    var timings = new List<string>();
                                    if (med.BitBbf == true) timings.Add("BBF");
                                    if (med.BitAbf == true) timings.Add("ABF");
                                    if (med.BitBl == true) timings.Add("BL");
                                    if (med.BitAl == true) timings.Add("AL");
                                    if (med.BitBd == true) timings.Add("BD");
                                    if (med.BitAd == true) timings.Add("AD");

                                    table.Cell().Padding(3).Text(timings.Count > 0 ? string.Join(", ", timings) : "N/A");
                                }

                                docCard.Item().PaddingTop(3).Text("Timing Abbreviations:").Bold().FontSize(8);
                                docCard.Item().Text("BBF = Before Breakfast, ABF = After Breakfast, BL = Before Lunch, AL = After Lunch, BD = Before Dinner, AD = After Dinner")
                                       .FontSize(7).FontColor(Colors.Black);
                            });
                        }

                        // Labs
                        if (labs.Any())
                        {
                            docCard.Item().PaddingTop(6).Text("LAB TESTS").Bold().FontSize(9);
                            foreach (var lab in labs)
                                docCard.Item().Text($"• {(lab.VchTestName ?? "").ToUpper()}").FontSize(8);
                        }

                        // Radiology
                        if (radiologies.Any())
                        {
                            docCard.Item().PaddingTop(6).Text("RADIOLOGY").Bold().FontSize(9);
                            foreach (var r in radiologies)
                                docCard.Item().Text($"• {(r.VchRadiologyName ?? "").ToUpper()}").FontSize(8);
                        }

                        // Procedures
                        if (procedures.Any())
                        {
                            docCard.Item().PaddingTop(6).Text("PROCEDURES").Bold().FontSize(9);
                            foreach (var p in procedures)
                                docCard.Item().Text($"• {(p.VchProcedureName ?? "").ToUpper()}").FontSize(8);
                        }

                        // Nutritional Consultation
                        docCard.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeColumn().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(card =>
                            {
                                card.Item().Text("NUTRITIONAL CONSULTATION").Bold().FontSize(8);
                                card.Item().Text(doctor?.BitNutritionalConsult == true ? "• YES" : "• NO").FontSize(8);
                            });

                            row.RelativeColumn().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(card =>
                            {
                                card.Item().Text("FOLLOW UP DATE").Bold().FontSize(8);
                                card.Item().Text(doctor?.DtFollowUpDate.HasValue == true
                                    ? doctor.DtFollowUpDate.Value.ToString("• dd/MM/yyyy")
                                    : "• NA").FontSize(8);
                            });
                        });

                        // Explanation + Signature
                        docCard.Item().PaddingTop(6).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(card =>
                        {
                            card.Item().Text("I have explained to patient the disease process, proposed plan of care and possible side effects in their own language.")
                                .Italic().FontSize(8);

                            card.Item().PaddingVertical(4);

                            card.Item().AlignCenter().Row(row =>
                            {
                                row.RelativeColumn();

                                row.ConstantColumn(100).Column(sig =>
                                {
                                    sig.Item().Height(50).Element(cell =>
                                    {
                                        if (!string.IsNullOrWhiteSpace(users?.VchSignFileName))
                                            cell.Image("wwwroot/Uploads/Signature/" + users.VchSignFileName, ImageScaling.FitWidth);
                                        else
                                            cell.Text("Signature missing").FontSize(8).Italic().FontColor(Colors.Red.Medium).AlignCenter();
                                    });

                                    sig.Item().AlignCenter().Text(
                                        string.IsNullOrWhiteSpace(nursing?.VchHmsconsultant) ? "N/A" : nursing.VchHmsconsultant.ToUpper()
                                    ).FontSize(8).Bold();
                                });
                            });
                        });

                        // Important Notes
                        docCard.Item().PaddingTop(6).Background(Colors.Grey.Lighten4).Padding(6).Column(note =>
                        {
                            note.Item().Text("NOTE:").Bold().FontSize(8);
                            note.Item().Text("• Kindly confirm your appointment one day prior at 01762-512666").FontSize(8);
                            note.Item().Text("• In case of emergency (Fever >101.5°F / 38.6°C, new/worsening pain, chest pain, breathlessness, vomiting, etc.), please contact 01762-512600 & Dr. Mayank Sharma (Reg No. 9273) at 01762-512600").FontSize(8);
                        });
                    });

                    // ===== FOOTER =====
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Generated on: ").SemiBold().FontSize(7);
                        txt.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm")).FontSize(7);
                    });
                });
            });
        }
    }
}   