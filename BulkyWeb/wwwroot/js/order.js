
let dataTable;
$(document).ready(function () {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/order/getall?status=' + new URLSearchParams(window.location.search)?.get("status") ?? "all" },
        "columns": [
            { data: 'id', width: '5%' },
            { data: 'name', width: '10%' },
            { data: 'phoneNumber', width: '10%' },
            { data: 'applicationUser.email', width: '20%' },
            { data: 'orderStatus', width: '20%' },
            { data: 'paymentStatus', width: '20%' },
            { data: 'orderTotal', width: '10%' },
            {
                data: 'id',
                width: '15%',
                render: (id) => {
                    return `<div class="btn-group w-100" role="group">
                                <a href="/admin/order/details?orderId=${id}" class="btn btn-primary w-50 mx-2">
                                    <i class="bi bi-pencil d-block"></i>
                                </a >
                            </div >`
                }
            }
        ], 
    });
});