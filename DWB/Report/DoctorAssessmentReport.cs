using DWB.APIModel;
using DWB.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection.Metadata;

namespace DWB.Report
{
    public class DoctorAssessmentReport :IDocument
    {       
        private readonly TblNsassessment nursing;
        private readonly TblDoctorAssessment doctor;
        private readonly List<TblDoctorAssmntMedicine> medicines;
        private readonly List<TblDoctorAssmntLab> labs;
        private readonly List<TblDoctorAssmntRadiology> radiologies;
        private readonly List<TblDoctorAssmntProcedure> procedures;

        public DoctorAssessmentReport(            
            TblNsassessment nursing,
            TblDoctorAssessment doctor,
            List<TblDoctorAssmntMedicine> medicines,
            List<TblDoctorAssmntLab> labs,
            List<TblDoctorAssmntRadiology> radiologies,
            List<TblDoctorAssmntProcedure> procedures)
        {
            this.nursing=nursing;            
            this.doctor = doctor;
            this.medicines = medicines ?? new List<TblDoctorAssmntMedicine>();
            this.labs = labs ?? new List<TblDoctorAssmntLab>();
            this.radiologies = radiologies ?? new List<TblDoctorAssmntRadiology>();
            this.procedures = procedures ?? new List<TblDoctorAssmntProcedure>();
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;



        //public void Compose(IDocumentContainer container)
        //{
        //    container.Page(page =>
        //    {
        //        page.Size(PageSizes.A4);
        //        page.Margin(30);

        //        // set default font size for whole page
        //        page.DefaultTextStyle(x => x.FontSize(9));


        //        // ===== HEADER =====
        //        page.Header().Row(row =>
        //        {
        //            row.RelativeColumn().Text("🏥 Indus International Hospital")
        //                .FontSize(16).Bold().FontColor(Colors.Blue.Medium);

        //            row.ConstantColumn(80).Height(50).Image("wwwroot/assets/images/LogoI2.png"); // for logo if needed
        //        });

        //        // ===== CONTENT =====
        //        page.Content().Column(col =>
        //        {
        //            // Title
        //            col.Item().AlignCenter().Text("Doctor Assessment Report")
        //                .FontSize(18).Bold().FontColor(Colors.Black);

        //            col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

        //            // Nursing Assessment Card
        //            col.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(nurse =>
        //            {
        //                nurse.Item().Text("🩺 NURSING ASSESSMENT").FontSize(12).SemiBold();
        //                nurse.Item().Text($"BLOOD PRESSURE: {nursing?.VchBloodPressure ?? "N/A"}");
        //                nurse.Item().Text($"PULSE: {nursing?.VchPulse ?? "N/A"}");
        //                nurse.Item().Text($"TEMPPERATURE: {nursing?.DecTemperature ?? "N/A"}");
        //            });

        //            col.Item().PaddingVertical(8);

        //            // Doctor Assessment Card
        //            col.Item().Background(Colors.LightBlue.Lighten4).Padding(10).Column(doc =>
        //            {
        //                doc.Item().Text("👨‍⚕️ DOCTOR ASSESSMENT").FontSize(14).SemiBold();
        //                doc.Item().Text($"COMPAINTS: {doctor?.VchChiefcomplaints ?? "N/A"}".ToUpper());
        //                doc.Item().Text($"DIAGNOSIS: {doctor?.VchDiagnosis ?? "N/A"}".ToUpper());
        //                doc.Item().Text($"REMARKS: {doctor?.VchRemarks ?? "N/A"}".ToUpper());
        //            });

        //            col.Item().PaddingVertical(8);

        //            // Medicines Table
        //            if (medicines.Any())
        //            {
        //                col.Item().Text("💊 PRESCRIBED MEDICINES").FontSize(14).SemiBold();

        //                col.Item().Table(table =>
        //                {
        //                    table.ColumnsDefinition(columns =>
        //                    {
        //                        columns.RelativeColumn(2); // Medicine
        //                        columns.RelativeColumn(1); // Dosage
        //                        columns.RelativeColumn(1); // Duration
        //                    });

        //                    // Table header
        //                    table.Header(header =>
        //                    {
        //                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("MEDICINE").SemiBold();
        //                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("DOSAGE").SemiBold();
        //                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("DURATION").SemiBold();
        //                    });

        //                    // Table rows
        //                    foreach (var med in medicines)
        //                    {
        //                        table.Cell().Padding(5).Text((med.VchMedicineName ?? "").ToUpper());
        //                        table.Cell().Padding(5).Text((med.VchDosage ?? "").ToUpper());
        //                        table.Cell().Padding(5).Text((med.VchDuration ?? "").ToUpper());
        //                    }
        //                });
        //            }

        //            col.Item().PaddingVertical(8);

        //            // Lab Tests
        //            if (labs.Any())
        //            {
        //                col.Item().Text("🧪 LAB TESTS").FontSize(14).SemiBold();
        //                foreach (var lab in labs)
        //                    col.Item().Text($"• {lab.VchTestName}".ToUpper());
        //            }

        //            col.Item().PaddingVertical(8);

        //            // Radiology
        //            if (radiologies.Any())
        //            {
        //                col.Item().Text("🩻 RADIOLOGY").FontSize(14).SemiBold();
        //                foreach (var r in radiologies)
        //                    col.Item().Text($"• {r.VchRadiologyName}".ToUpper());
        //            }

        //            col.Item().PaddingVertical(8);

        //            // Procedures
        //            if (procedures.Any())
        //            {
        //                col.Item().Text("🔧 PROCEDURES").FontSize(14).SemiBold();
        //                foreach (var p in procedures)
        //                    col.Item().Text($"• {p.VchProcedureName}".ToUpper());
        //            }
        //        });

        //        // ===== FOOTER =====
        //        page.Footer().AlignCenter().Text(txt =>
        //        {
        //            txt.Span("Generated on: ").SemiBold();
        //            txt.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm"));
        //        });
        //    });
        //}

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);

                // Set default font size
                page.DefaultTextStyle(x => x.FontSize(9));

                // ===== HEADER =====
                page.Header().Row(row =>
                {
                    row.RelativeColumn().Text("🏥 Indus International Hospital")
                        .FontSize(16).Bold().FontColor(Colors.Blue.Medium);

                    row.ConstantColumn(80).Height(50).Image("wwwroot/assets/images/LogoI2.png");
                });

                // ===== CONTENT =====
                page.Content().Column(col =>
                {
                    // Title
                    col.Item().AlignCenter().Text("DOCTOR ASSESSMENT REPORT")
                        .FontSize(18).Bold().FontColor(Colors.Black);

                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);



                    // ===== Patient & Consultant Cards =====
                    col.Item().Row(row =>
                    {
                        // Patient Details Card
                        row.RelativeColumn().Background(Colors.White)
                            .Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Padding(10).PaddingRight(5) // Padding to the right for spacing
                            .Column(patient =>
                            {
                                patient.Item().Text("PATIENT DETAILS").Bold().FontSize(12);
                                patient.Item().Text($"NAME : {nursing?.VchHmsname?.ToUpper() ?? "N/A"}");
                                patient.Item().Text($"AGE : {nursing?.IntAge ?? 0}  |  GENDER : {nursing?.VchGender?.ToUpper() ?? "N/A"}");
                                patient.Item().Text($"VISIT : {nursing?.IntIhmsvisit??0} | DATE : {nursing?.DtCreated.Value.ToString("dd/MM/yyyy")}");
                                patient.Item().Text($"CONSULTANT : {nursing?.VchHmsconsultant?.ToUpper() ?? "N/A"}");
                            });

                        // Nursing assessment Details Card
                        row.RelativeColumn().Background(Colors.White)
                            .Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Padding(10).PaddingLeft(5) // Margin to the left for spacing
                            .Column(nu =>
                            {
                                nu.Item().Text("NURSING ASSESSMENT").Bold().FontSize(12);
                                nu.Item().Text($"BP : {nursing?.VchBloodPressure ?? "N/A"} | SpO2 : {nursing?.DecSpO2??0}");
                                nu.Item().Text($"PULSE : {nursing?.VchPulse ?? "N/A"} | Height : {nursing?.DecHeight??0}");
                                nu.Item().Text($"TEMP : {nursing?.DecTemperature ?? "N/A"} | RespiratoryRate : {nursing?.DecRespiratoryRate??0}");
                            });
                    });


                    col.Item().PaddingVertical(8);
                    // Doctor Assessment Header
                    col.Item().Text("👨‍⚕️DOCTOR ASSESSMENT").FontSize(14).SemiBold();
                    // ===== Doctor Assessment Details =====
                    col.Item().Background(Colors.LightBlue.Lighten4).Padding(10).Column(doc =>
                    {
                        doc.Item().Text($"COMPLAINTS: {(doctor?.VchChiefcomplaints ?? "N/A").ToUpper()}");
                        doc.Item().Text($"DIAGNOSIS: {(doctor?.VchDiagnosis ?? "N/A").ToUpper()}");
                        doc.Item().Text($"REMARKS: {(doctor?.VchRemarks ?? "N/A").ToUpper()}");
                    });

                    col.Item().PaddingVertical(8);

                    // ===== Medicines Table =====
                    if (medicines.Any())
                    {
                        col.Item().Text("💊PRESCRIBED MEDICINES").FontSize(14).SemiBold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Medicine
                                columns.RelativeColumn(1); // Dosage
                                columns.RelativeColumn(1); // Duration
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("MEDICINE").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("DOSAGE").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("DURATION").SemiBold();
                            });

                            foreach (var med in medicines)
                            {
                                table.Cell().Padding(5).Text((med.VchMedicineName ?? "").ToUpper());
                                table.Cell().Padding(5).Text((med.VchDosage ?? "").ToUpper());
                                table.Cell().Padding(5).Text((med.VchDuration ?? "").ToUpper());
                            }
                        });
                    }

                    col.Item().PaddingVertical(8);

                    // ===== Lab Tests =====
                    if (labs.Any())
                    {
                        col.Item().Text("🧪LAB TESTS").FontSize(14).SemiBold();
                        foreach (var lab in labs)
                            col.Item().Text($"• {(lab.VchTestName ?? "").ToUpper()}");
                    }

                    col.Item().PaddingVertical(8);

                    // ===== Radiology =====
                    if (radiologies.Any())
                    {
                        col.Item().Text("🩻RADIOLOGY").FontSize(14).SemiBold();
                        foreach (var r in radiologies)
                            col.Item().Text($"• {(r.VchRadiologyName ?? "").ToUpper()}");
                    }

                    col.Item().PaddingVertical(8);

                    // ===== Procedures =====
                    if (procedures.Any())
                    {
                        col.Item().Text("PROCEDURES").FontSize(14).SemiBold();
                        foreach (var p in procedures)
                            col.Item().Text($"• {(p.VchProcedureName ?? "").ToUpper()}");
                    }
                });

                // ===== FOOTER =====
                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Generated on: ").SemiBold();
                    txt.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm"));
                });
            });
        }




    }
}
