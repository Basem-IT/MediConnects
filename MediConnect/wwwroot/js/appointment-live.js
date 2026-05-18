console.log("appointment-live.js loaded");

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/appointments")
    .build();
connection.on("AppointmentStatusUpdated", function (appointment) {
    const row = document.getElementById("appointment-" + appointment.appointmentId);
    if (row) {
        row.querySelector(".status-cell").innerText = appointment.status;
    }
});
connection.start()
    .then(function () {
        console.log("Connected to AppointmentHub");
    })
    .catch(function (err) {
        console.error("SignalR connection error:", err);
    });


document.getElementById("testUpdateBtn").addEventListener("click", function () {
    const testAppointment = {
        appointmentId: 1,
        status: "Checked-In"
    };



    console.log("Testing update", testAppointment);


    const row = document.getElementById("appointment-" + testAppointment.appointmentId);

    if (row) {
        row.querySelector(".status-cell").innerText = testAppointment.status;
    }





});