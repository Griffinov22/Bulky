let dataTable;
$(document).ready(function () {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/user/getall' },
        "columns": [
            { data: 'name', width: '20%' },
            { data: 'email', width: '15%' },
            { data: 'phoneNumber', width: '15%' },
            { data: 'company.name', width: '10%' },
            { data: 'role', width: '15%' },
            {
                data: {id:'id', lockoutEnd: 'lockoutEnd'},
                width: '30%',
                render: ({id, lockoutEnd}) => {
                    const today = new Date().getTime();
                    const lockoutDate = new Date(lockoutEnd).getTime();
                    let locked = false;

                    if (lockoutDate > today) locked = true;

                    return `
                    <div class="text-center">
                        <a onclick=LockUnlock('${id}') class="btn btn-${locked ? "success" : "danger"} text-white text-nowrap m-0" style="cursor: pointer; width: 40%">
                            <i class="bi bi-${locked ? "unlock" : "lock"}-fill"></i> ${locked ? "Unlock" : "Lock"}
                        </a>
                        <a href="/Admin/User/RoleManagement?userId=${id}" class="btn btn-danger text-white text-nowrap m-0" style="cursor: pointer; width: 40%">
                            <i class="bi bi-pencil-fill"></i> Permission
                        </a>
                    </div>
                    `;
                    
                }
            }
        ],
    });
});

const LockUnlock = (id) => {
    $.ajax({
        type: "POST",
        url: "/Admin/User/LockUnlock",
        data: JSON.stringify(id),
        contentType: "application/json",
        success: function (data) {
            if (data.success) {
                toastr.success(data.message);
                dataTable.ajax.reload();
            }
        }
    });
};