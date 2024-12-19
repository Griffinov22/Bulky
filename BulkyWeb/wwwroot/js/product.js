
let dataTable;
$(document).ready(function () {
    dataTable = $('#tblData').DataTable({
        "ajax": {url:'/admin/product/getall'},
        "columns": [
            { data: 'title', width: '25%' },
            { data: 'isbn', width: '15%' },
            { data: 'price', width: '10%' },
            { data: 'author', width: '15%' },
            { data: 'category.name', width: '10%' },
            {
                data: 'id',
                width: '25%',
                render: (id) => {
                    return `<div class="btn-group w-100" role="group">
                                <a href="/admin/product/upsert/${id}" class="btn btn-primary w-50 mx-2">
                                    <i class="bi bi-pencil d-block"></i> Edit
                                </a >

                                <a OnClick=Delete("/admin/product/delete/${id}") class="btn btn-danger w-50 mx-2">
                                    <i class="bi bi-trash d-block"></i> Delete
                                </a>
                            </div >`
                }
            }
        ], 
    });
});

function Delete(url) {
    Swal.fire({
        title: "Are you sure?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url,
                type: 'DELETE',
                success: (data) => {
                    dataTable.ajax.reload();
                    toastr.success(data.message);
                }
            });
        }
    });
}
