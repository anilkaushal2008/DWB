using DWB.APIModel;
using DWB.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.Xml;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        public DoctorAssessmentReport(
            TblNsassessment nursing,
            TblDoctorAssessment doctor,
            List<TblDoctorAssmntMedicine> medicines,
            List<TblDoctorAssmntLab> labs,
            List<TblDoctorAssmntRadiology> radiologies,
            List<TblDoctorAssmntProcedure> procedures,
            TblUsers users,
            IndusCompanies companies)
        {
            this.nursing = nursing;
            this.doctor = doctor;
            this.medicines = medicines ?? new List<TblDoctorAssmntMedicine>();
            this.labs = labs ?? new List<TblDoctorAssmntLab>();
            this.radiologies = radiologies ?? new List<TblDoctorAssmntRadiology>();
            this.procedures = procedures ?? new List<TblDoctorAssmntProcedure>();
            this.users = users;
            this.companies = companies;
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

        //public void Compose(IDocumentContainer container)
        //{
        //    container.Page(page =>
        //    {
        //        page.Size(PageSizes.A4);
        //        page.Margin(10);

        //        // Default small font
        //        page.DefaultTextStyle(x => x.FontSize(8));

        //        // ===== HEADER =====
        //        page.Header().PaddingBottom(2).Column(headerCol =>
        //        {
        //            headerCol.Item().Row(row =>
        //            {
        //                // Centered Hospital Name + Address
        //                row.RelativeColumn().AlignCenter().Column(col =>
        //                {
        //                    col.Item().Text(companies.Descript)
        //                        .FontSize(14).Bold().FontColor(Colors.Black);

        //                    col.Item().Text(companies.Add1)
        //                        .FontSize(8)
        //                        .FontColor(Colors.Grey.Darken1)
        //                        .AlignCenter();

        //                    col.Item().Text(companies.Add2)
        //                        .FontSize(8)
        //                        .FontColor(Colors.Grey.Darken1)
        //                        .AlignCenter();
        //                });
        //            });

        //            // 🔹 Horizontal line
        //            headerCol.Item()
        //                .PaddingVertical(4)
        //                .LineHorizontal(1)
        //                .LineColor(Colors.Grey.Lighten2);
        //        });


        //        // ===== CONTENT =====
        //        page.Content().PaddingTop(0).Column(col =>
        //            {
        //                // Main Title
        //                col.Item().AlignCenter().Text("DOCTOR ASSESSMENT REPORT")
        //                 .FontSize(12).Bold().FontColor(Colors.Black);   // 🔽 no extra padding on top

        //                //col.Item().PaddingVertical(4).LineHorizontal(0.8).LineColor(Colors.Grey.Lighten2);

        //                // ===== Patient Details Card =====
        //                col.Item().Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(patient =>
        //                {
        //                    patient.Item().Text("PATIENT DETAILS").Bold().FontSize(9);
        //                    patient.Item().Text($"NAME : {nursing?.VchHmsname?.ToUpper() ?? "N/A"}");
        //                    patient.Item().Text($"AGE : {nursing?.IntAge ?? 0}  |  GENDER : {nursing?.VchGender?.ToUpper() ?? "N/A"}");
        //                    patient.Item().Text($"VISIT : {nursing?.IntIhmsvisit ?? 0} | DATE : {nursing?.DtCreated.Value.ToString("dd/MM/yyyy")}");
        //                    patient.Item().Text($"CONSULTANT : {nursing?.VchHmsconsultant?.ToUpper() ?? "N/A"}");
        //                });

        //                col.Item().PaddingVertical(4);

        //                // ===== Nursing & Doctor Assessment Row Cards =====
        //                col.Item().Row(row =>
        //                {
        //                    // Nursing Assessment Card
        //                    row.RelativeColumn().Padding(5).Border(1).BorderColor(Colors.Grey.Lighten2)
        //                        .Background(Colors.White).Column(nursingCard =>
        //                        {
        //                            nursingCard.Item().Text("NURSING ASSESSMENT").Bold().FontSize(9);
        //                            nursingCard.Item().Text($"BP : {nursing?.VchBloodPressure ?? "N/A"} | SpO2 : {nursing?.DecSpO2 ?? 0}");
        //                            nursingCard.Item().Text($"PULSE : {nursing?.VchPulse ?? "N/A"} | HEIGHT : {nursing?.DecHeight ?? 0}");
        //                            nursingCard.Item().Text($"TEMP : {nursing?.DecTemperature ?? "N/A"} | RESPIRATORY RATE : {nursing?.DecRespiratoryRate ?? 0}");
        //                            nursingCard.Item().Text(nursing?.BitIsAllergical == true
        //                        ? $"ALLERGIES : YES | ALLERGY SOURCE : {(string.IsNullOrWhiteSpace(nursing?.VchAllergicalDrugs) ? "N/A" : nursing.VchAllergicalDrugs.ToUpper())}"
        //                            : "ALLERGIES : NO");
        //                            nursingCard.Item().Text($"ALCOHOL : {(nursing?.BitIsAlcoholic == true ? "YES" : "NO")} | " +
        //                           $"SMOKING : {(nursing?.BitIsSmoking == true ? "YES" : "NO")}");
        //                            nursingCard.Item().Text($"FALL RISK : {(nursing?.BitFallRisk == true ? "YES" : "NO")}");
        //                        });

        //                    row.Spacing(5); // gap between cards

        //                    // Doctor Assessment Card
        //                    row.RelativeColumn().Padding(5).Border(1).BorderColor(Colors.Grey.Lighten2)
        //                        .Background(Colors.White).Column(docCard =>
        //                        {
        //                            docCard.Item().Text("DOCTOR ASSESSMENT").Bold().FontSize(9);
        //                            docCard.Item().Text($"COMPLAINTS: {(doctor?.VchChiefcomplaints ?? "N/A").ToUpper()}");
        //                            docCard.Item().Text($"DIAGNOSIS: {(doctor?.VchDiagnosis ?? "N/A").ToUpper()}");
        //                            docCard.Item().Text($"REMARKS: {(doctor?.VchRemarks ?? "N/A").ToUpper()}");
        //                        });
        //                });





        //                // ===== Nursing & Doctor Assessment Card =====
        //                col.Item().Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(docCard =>
        //                {                            
        //                    // Medicines
        //                    if (medicines.Any())
        //                    {
        //                        docCard.Item().Text("PRESCRIBED MEDICINES").Bold().FontSize(9);

        //                        docCard.Item().Table(table =>
        //                        {
        //                            table.ColumnsDefinition(columns =>
        //                            {
        //                                columns.RelativeColumn(4);
        //                                columns.RelativeColumn(1);
        //                                columns.RelativeColumn(1);
        //                                columns.RelativeColumn(1);
        //                                columns.RelativeColumn(3);
        //                            });

        //                            table.Header(header =>
        //                            {
        //                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("MEDICINE").SemiBold();
        //                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("DOSAGE").SemiBold();
        //                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("FREQUENCY").SemiBold();
        //                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("DURATION").SemiBold();
        //                                header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("TIMING").SemiBold();
        //                            });

        //                            foreach (var med in medicines)
        //                            {
        //                                table.Cell().Padding(3).Text((med.VchMedicineName ?? "").ToUpper());
        //                                table.Cell().Padding(3).Text((med.VchDosage ?? "").ToUpper());
        //                                table.Cell().Padding(3).Text((med.VchFrequency ?? "").ToUpper());
        //                                table.Cell().Padding(3).Text((med.VchDuration ?? "").ToUpper());

        //                                var timings = new List<string>();
        //                                if (med.BitBbf == true) timings.Add("BBF");
        //                                if (med.BitAbf == true) timings.Add("ABF");
        //                                if (med.BitBl == true) timings.Add("BL");
        //                                if (med.BitAl == true) timings.Add("AL");
        //                                if (med.BitBd == true) timings.Add("BD");
        //                                if (med.BitAd == true) timings.Add("AD");

        //                                table.Cell().Padding(3).Text(timings.Count > 0 ? string.Join(", ", timings) : "N/A");
        //                            }

        //                            docCard.Item().PaddingTop(3).Text("Timing Abbreviations:").Bold().FontSize(8);
        //                            docCard.Item().Text("BBF = Before Breakfast, ABF = After Breakfast, BL = Before Lunch, AL = After Lunch, BD = Before Dinner, AD = After Dinner")
        //                                   .FontSize(8).FontColor(Colors.Black);
        //                        });
        //                    }

        //                    // Labs
        //                    if (labs.Any())
        //                    {
        //                        docCard.Item().PaddingTop(6).Text("LAB TESTS").Bold().FontSize(9);
        //                        foreach (var lab in labs)
        //                            docCard.Item().Text($"• {(lab.VchTestName ?? "").ToUpper()}").FontSize(8);
        //                    }

        //                    // Radiology
        //                    if (radiologies.Any())
        //                    {
        //                        docCard.Item().PaddingTop(6).Text("RADIOLOGY").Bold().FontSize(9);
        //                        foreach (var r in radiologies)
        //                            docCard.Item().Text($"• {(r.VchRadiologyName ?? "").ToUpper()}").FontSize(8);
        //                    }

        //                    // Procedures
        //                    if (procedures.Any())
        //                    {
        //                        docCard.Item().PaddingTop(6).Text("PROCEDURES").Bold().FontSize(9);
        //                        foreach (var p in procedures)
        //                            docCard.Item().Text($"• {(p.VchProcedureName ?? "").ToUpper()}").FontSize(8);
        //                    }

        //                    // Nutritional Consultation
        //                    docCard.Item().PaddingTop(6).Row(row =>
        //                    {
        //                        row.RelativeColumn().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(card =>
        //                        {
        //                            card.Item().Text("NUTRITIONAL CONSULTATION").Bold().FontSize(8);
        //                            card.Item().Text(doctor?.BitNutritionalConsult == true ? "• YES" : "• NO").FontSize(8);
        //                        });

        //                        row.RelativeColumn().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(card =>
        //                        {
        //                            card.Item().Text("FOLLOW UP DATE").Bold().FontSize(8);
        //                            card.Item().Text(doctor?.DtFollowUpDate.HasValue == true
        //                                ? doctor.DtFollowUpDate.Value.ToString("• dd/MM/yyyy")
        //                                : "• NA").FontSize(8);
        //                        });
        //                    });

        //                    // Explanation + Signature
        //                    docCard.Item().PaddingTop(6).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(card =>
        //                    {
        //                        card.Item().Text("I have explained to patient the disease process, proposed plan of care and possible side effects in their own language.")
        //                            .Italic().FontSize(8);

        //                        card.Item().PaddingVertical(4);

        //                        card.Item().AlignCenter().Row(row =>
        //                        {
        //                            row.RelativeColumn();

        //                            row.ConstantColumn(100).Column(sig =>
        //                            {
        //                                sig.Item().Height(50).Element(cell =>
        //                                {
        //                                    if (!string.IsNullOrWhiteSpace(users?.VchSignFileName))
        //                                        cell.Image("wwwroot/Uploads/Signature/" + users.VchSignFileName, ImageScaling.FitWidth);
        //                                    else
        //                                        cell.Text("Signature missing").FontSize(8).Italic().FontColor(Colors.Red.Medium).AlignCenter();
        //                                });

        //                                sig.Item().AlignCenter().Text(
        //                                    string.IsNullOrWhiteSpace(nursing?.VchHmsconsultant) ? "N/A" : nursing.VchHmsconsultant.ToUpper()
        //                                ).FontSize(8).Bold();
        //                            });
        //                        });
        //                    });

        //                    // Important Notes
        //                    docCard.Item().PaddingTop(6).Background(Colors.Grey.Lighten4).Padding(6).Column(note =>
        //                    {
        //                        note.Item().Text("NOTE:").Bold().FontSize(8);
        //                        note.Item().Text("• Kindly confirm your appointment one day prior at 01762-512666").FontSize(8);
        //                        note.Item().Text("• In case of emergency (Fever >101.5°F / 38.6°C, new/worsening pain, chest pain, breathlessness, vomiting, etc.), please contact 01762-512600 & Dr. Mayank Sharma (Reg No. 9273) at 01762-512600").FontSize(8);
        //                    });
        //                });

        //                // ===== FOOTER ===== wwwroot/assets/images/footer.jpg
        //                page.Footer().AlignCenter().Text(txt =>
        //                {
        //                    txt.Span("Print on: ").SemiBold().FontSize(7);
        //                    txt.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm")).FontSize(7);
        //                });
        //            });
        //    });
        //}

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
                    headerCol.Item()
                        .PaddingVertical(4)
                        .LineHorizontal(1)
                        .LineColor(Colors.Black);
                });

                // ===== CONTENT =====
                page.Content().PaddingTop(0).Column(col =>
                {
                    // Main Title
                    col.Item().PaddingBottom(2).AlignCenter().Text("DOCTOR ASSESSMENT REPORT")
                        .FontSize(12).Bold().FontColor(Colors.Black);

                    // ===== Patient Details Card =====
                    col.Item().Background(Colors.White)
                        .Border(1).BorderColor(Colors.Black)
                        .Padding(8).Table(table =>
                        {
                            // Column setup
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                            });

                            // === Row 1: Headers ===
                            table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text("UHID").Bold().FontSize(8);
                            table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text("NAME").Bold().FontSize(8);
                            table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text("AGE / GENDER").Bold().FontSize(8);

                            // === Row 2: Details ===
                            table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(nursing?.VchUhidNo ?? "N/A").FontSize(8);
                            table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(nursing?.VchHmsname?.ToUpper() ?? "N/A").FontSize(8);
                            table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text($"{(nursing?.IntAge ?? 0)} / {nursing?.VchGender?.ToUpper() ?? "N/A"}").FontSize(8);

                            // === Row 3: Headers ===
                            table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text("DATE").Bold().FontSize(8);
                            table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black).Padding(4).Text("CONSULTANT").Bold().FontSize(8);

                            // === Row 4: Details ===
                            table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(nursing?.DtCreated?.ToString("dd/MM/yyyy") ?? "N/A").FontSize(8);
                            table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black).Padding(4).Text(nursing?.VchHmsconsultant?.ToUpper() ?? "N/A").FontSize(8);
                        });

                    col.Item().PaddingVertical(4);

                    // ===== Nursing & Doctor Assessment Row Cards (consistent border) =====
                    col.Item().Row(row =>
                    {
                        // ===== Nursing Assessment Table =====
                        row.RelativeColumn()
                            .Padding(0) // removed outer card padding/border
                            .Table(table =>
                            {
                                // Column setup
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                // ===== Title row =====
                                table.Cell().ColumnSpan(4)
            .Background(Colors.Grey.Lighten3)
            .Border(1).BorderColor(Colors.Black).Padding(4)
            .Text("NURSING ASSESSMENT").Bold().FontSize(9).AlignCenter();

                                // ===== Row 1: Headers =====
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text("BP").Bold().FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text("SpO2").Bold().FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text("Pulse").Bold().FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text("Height").Bold().FontSize(8);

                                // ===== Row 2: Details =====
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(nursing?.VchBloodPressure ?? "N/A").FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text($"{nursing?.DecSpO2 ?? 0}").FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(nursing?.VchPulse ?? "N/A").FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text($"{nursing?.DecHeight ?? 0}").FontSize(8);

                                // ===== Row 3: Headers =====
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text("Temp").Bold().FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text("Respiratory Rate").Bold().FontSize(8);
                                table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black).Padding(4).Text("Allergies").Bold().FontSize(8);

                                // ===== Row 4: Details =====
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text($"{nursing?.DecTemperature ?? "N/A"}").FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text($"{nursing?.DecRespiratoryRate ?? 0}").FontSize(8);
                                table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black).Padding(4).Text(
                                 nursing?.BitIsAllergical == true
                                 ? $"YES ({(string.IsNullOrWhiteSpace(nursing?.VchAllergicalDrugs) ? "N/A" : nursing.VchAllergicalDrugs.ToUpper())})"                              : "NO"
                      ).FontSize(8);

                                // ===== Row 5: Headers =====
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text("Alcohol").Bold().FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text("Smoking").Bold().FontSize(8);
                                table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black).Padding(4).Text("Fall Risk").Bold().FontSize(8);

                                // ===== Row 6: Details =====
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(nursing?.BitIsAlcoholic == true ? "YES" : "NO").FontSize(8);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(nursing?.BitIsSmoking == true ? "YES" : "NO").FontSize(8);
                                table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black).Padding(4).Text(nursing?.BitFallRisk == true ? "YES" : "NO").FontSize(8);
                            });



                        row.Spacing(6);

                        // ===== Doctor Assessment Card =====
                        row.RelativeColumn().Background(Colors.White)
                            .Border(1).BorderColor(Colors.Black)
                            .Padding(0).Column(docCard =>
                            {
                                docCard.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1); // Header
                                        columns.RelativeColumn(3); // Details
                                    });

                                    // Title row spanning both columns
                                    table.Cell().ColumnSpan(2)
                                        .Background(Colors.Grey.Lighten3)
                                        .Border(1).BorderColor(Colors.Black).Padding(4)
                                        .Text("DOCTOR ASSESSMENT").Bold().FontSize(9).AlignCenter();

                                    // Row 1 - Complaints
                                    table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                                         .Text("COMPLAINTS").Bold().FontSize(8);
                                    table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                                         .Text($"{doctor?.VchChiefcomplaints?.ToUpper() ?? "N/A"}").FontSize(8);

                                    // Row 2 - Diagnosis
                                    table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                                         .Text("DIAGNOSIS").Bold().FontSize(8);
                                    table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                                         .Text($"{doctor?.VchDiagnosis?.ToUpper() ?? "N/A"}").FontSize(8);

                                    // Row 3 - Remarks
                                    table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                                         .Text("REMARKS").Bold().FontSize(8);
                                    table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                                         .Text($"{doctor?.VchRemarks?.ToUpper() ?? "N/A"}").FontSize(8);
                                });
                            });




                    });


                    col.Item().PaddingVertical(4);
                    // ===== Labs, Radiology & Procedures Combined Card =====
                    if (labs.Any() || radiologies.Any() || procedures.Any())
                    {
                        col.Item().PaddingTop(0).Background(Colors.White)
                            .Border(1).BorderColor(Colors.Black)
                            .Padding(10).Column(card =>
                            {
                                // Labs
                                if (labs.Any())
                                {
                                    card.Item().Text("LAB TESTS").Bold().FontSize(9);
                                    foreach (var lab in labs)
                                        card.Item().Text($"• {(lab.VchTestName ?? "").ToUpper()}").FontSize(8);

                                    card.Item().PaddingBottom(4); // space before next section
                                }

                                // Radiology
                                if (radiologies.Any())
                                {
                                    card.Item().Text("RADIOLOGY").Bold().FontSize(9);
                                    foreach (var r in radiologies)
                                        card.Item().Text($"• {(r.VchRadiologyName ?? "").ToUpper()}").FontSize(8);

                                    card.Item().PaddingBottom(4);
                                }

                                // Procedures
                                if (procedures.Any())
                                {
                                    card.Item().Text("PROCEDURES").Bold().FontSize(9);
                                    foreach (var p in procedures)
                                        card.Item().Text($"• {(p.VchProcedureName ?? "").ToUpper()}").FontSize(8);
                                }
                            });
                    }

                    col.Item().PaddingVertical(4);
                    // ===== Medicines Card =====
                    if (medicines.Any())
                    {
                        col.Item().Background(Colors.White)
                            .Border(1).BorderColor(Colors.Black)
                            .Padding(10).Column(docCard =>
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

                                    // ===== Header Row with Borders =====
                                    table.Header(header =>
                                    {
                                        header.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text("MEDICINE").SemiBold().FontSize(7);
                                        header.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text("DOSAGE").SemiBold().FontSize(7);
                                        header.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text("FREQUENCY").SemiBold().FontSize(7);
                                        header.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text("DURATION").SemiBold().FontSize(7);
                                        header.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text("TIMING").SemiBold().FontSize(7);
                                    });

                                    // ===== Medicine Rows with Borders =====
                                    foreach (var med in medicines)
                                    {
                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text((med.VchMedicineName ?? "").ToUpper()).FontSize(7);

                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text((med.VchDosage ?? "").ToUpper()).FontSize(7);

                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text((med.VchFrequency ?? "").ToUpper()).FontSize(7);

                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text((med.VchDuration ?? "").ToUpper()).FontSize(7);

                                        var timings = new List<string>();
                                        if (med.BitBbf == true) timings.Add("BBF");
                                        if (med.BitAbf == true) timings.Add("ABF");
                                        if (med.BitBl == true) timings.Add("BL");
                                        if (med.BitAl == true) timings.Add("AL");
                                        if (med.BitBd == true) timings.Add("BD");
                                        if (med.BitAd == true) timings.Add("AD");

                                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(3)
                                            .Text(timings.Count > 0 ? string.Join(", ", timings) : "N/A").FontSize(7);
                                    }
                                });

                                docCard.Item().PaddingTop(4).Text("Timing Abbreviations:").Bold().FontSize(7);
                                docCard.Item().Text("BBF = Before Breakfast, ABF = After Breakfast, BL = Before Lunch, AL = After Lunch, BD = Before Dinner, AD = After Dinner")
                                    .FontSize(7).FontColor(Colors.Black);
                            });
                    }

                    //    // ===== Medicines Card =====
                    //    if (medicines.Any())
                    //    {
                    //        col.Item().Background(Colors.White)
                    //            .Border(1).BorderColor(Colors.Black)
                    //            .Padding(10).Column(docCard =>
                    //            {
                    //                docCard.Item().Text("PRESCRIBED MEDICINES").Bold().FontSize(9);

                    //                docCard.Item().Table(table =>
                    //                {
                    //                    table.ColumnsDefinition(columns =>
                    //                    {
                    //                        columns.RelativeColumn(4);
                    //                        columns.RelativeColumn(1);
                    //                        columns.RelativeColumn(1);
                    //                        columns.RelativeColumn(1);
                    //                        columns.RelativeColumn(3);
                    //                    });

                    //                    table.Header(header =>
                    //                    {
                    //                        header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("MEDICINE").SemiBold();
                    //                        header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("DOSAGE").SemiBold();
                    //                        header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("FREQUENCY").SemiBold();
                    //                        header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("DURATION").SemiBold();
                    //                        header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("TIMING").SemiBold();
                    //                    });

                    //                    foreach (var med in medicines)
                    //                    {
                    //                        table.Cell().Padding(3).Text((med.VchMedicineName ?? "").ToUpper());
                    //                        table.Cell().Padding(3).Text((med.VchDosage ?? "").ToUpper());
                    //                        table.Cell().Padding(3).Text((med.VchFrequency ?? "").ToUpper());
                    //                        table.Cell().Padding(3).Text((med.VchDuration ?? "").ToUpper());

                    //                        var timings = new List<string>();
                    //                        if (med.BitBbf == true) timings.Add("BBF");
                    //                        if (med.BitAbf == true) timings.Add("ABF");
                    //                        if (med.BitBl == true) timings.Add("BL");
                    //                        if (med.BitAl == true) timings.Add("AL");
                    //                        if (med.BitBd == true) timings.Add("BD");
                    //                        if (med.BitAd == true) timings.Add("AD");

                    //                        table.Cell().Padding(3).Text(timings.Count > 0 ? string.Join(", ", timings) : "N/A");
                    //                    }
                    //                });

                    //                docCard.Item().PaddingTop(4).Text("Timing Abbreviations:").Bold().FontSize(8);
                    //                docCard.Item().Text("BBF = Before Breakfast, ABF = After Breakfast, BL = Before Lunch, AL = After Lunch, BD = Before Dinner, AD = After Dinner")
                    //.FontSize(8).FontColor(Colors.Black);
                    //            });
                    //    }

                    // ===== Nutritional Consultation + Follow Up Card =====
                    col.Item().PaddingTop(6).Background(Colors.White)
                        .Border(1).BorderColor(Colors.Black)
                        .Padding(10).Row(row =>
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
                        .Padding(10).Column(card =>
                        {
                            // Explanation text
                            card.Item().Text("I have explained to the patient the disease process, proposed plan of care, and possible side effects in their own language.")
                            .Italic().FontSize(9);

                            card.Item().PaddingVertical(5);

                            // Row where signature sits at the right edge
                            card.Item().Row(row =>
                            {
                                row.RelativeColumn(); // consumes available space, pushing signature to the right

                                // Fixed-width column for signature to avoid layout conflicts
                                row.ConstantColumn(120).Column(sig =>
                                {
                                    // Signature image area (fixed height)
                                    sig.Item().Height(50).Element(cell =>
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
                            .Padding(8)
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

