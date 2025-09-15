using DWB.APIModel;
using DWB.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);

                page.Header().Text("Doctor Assessment Report")
                    .FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

                page.Content().Column(col =>
                {
                    // 🏥 Nursing Assessment
                    col.Item().Text("Nursing Assessment").FontSize(14).SemiBold();
                    col.Item().Text($"Blood Pressure: {nursing?.VchBloodPressure ?? "N/A"}");
                    col.Item().Text($"Pulse: {nursing?.VchPulse ?? "N/A"}");
                    col.Item().Text($"Temperature: {nursing?.DecTemperature ?? "N/A"}");

                    col.Item().PaddingVertical(10);

                    // 👨‍⚕️ Doctor Assessment
                    col.Item().Text("Doctor Assessment").FontSize(14).SemiBold();
                    col.Item().Text($"Complaints: {doctor?.VchChiefcomplaints ?? "N/A"}");
                    col.Item().Text($"Diagnosis: {doctor?.VchDiagnosis ?? "N/A"}");
                    col.Item().Text($"Remark: {doctor?.VchRemarks ?? "N/A"}");

                    col.Item().PaddingVertical(10);

                    // 💊 Medicines
                    if (medicines.Any())
                    {
                        col.Item().Text("Medicines").FontSize(14).SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Medicine");
                                header.Cell().Text("Dosage");
                                header.Cell().Text("Duration");
                            });

                            foreach (var med in medicines)
                            {
                                table.Cell().Text(med.VchMedicineName);
                                table.Cell().Text(med.VchDosage);
                                table.Cell().Text(med.VchDuration);
                            }
                        });
                    }

                    col.Item().PaddingVertical(10);

                    // 🧪 Labs
                    if (labs.Any())
                    {
                        col.Item().Text("Lab Tests").FontSize(14).SemiBold();
                        foreach (var lab in labs)
                            col.Item().Text($"- {lab.VchTestName}");
                    }

                    col.Item().PaddingVertical(10);

                    // 🩻 Radiology
                    if (radiologies.Any())
                    {
                        col.Item().Text("Radiology").FontSize(14).SemiBold();
                        foreach (var r in radiologies)
                            col.Item().Text($"- {r.VchRadiologyName}");
                    }

                    col.Item().PaddingVertical(10);

                    // 🔧 Procedures
                    if (procedures.Any())
                    {
                        col.Item().Text("Procedures").FontSize(14).SemiBold();
                        foreach (var p in procedures)
                            col.Item().Text($"- {p.VchProcedureName}");
                    }
                });

                page.Footer()
                    .AlignCenter()
                    .Text(txt =>
                    {
                        txt.Span("Generated on: ").SemiBold();
                        txt.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm"));
                    });
            });
        }

    }
}
