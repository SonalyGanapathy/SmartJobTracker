import { useState } from "react";

function AddJob() {
    const [job, setJob] = useState({
        company: "",
        role: "",
        status: ""
    });

    const handleChange = (e) => {
        setJob({ ...job, [e.target.name]: e.target.value });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        await fetch("http://localhost:5081/api/jobs", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(job)
        });
        window.location.reload(); // simple fix to refresh the job list after adding a new job
        alert("Job Added!");
    };

    return (
        <form onSubmit={handleSubmit}>
            <input name="company" placeholder="Company" onChange={handleChange} />
            <input name="role" placeholder="Role" onChange={handleChange} />
            <input name="status" placeholder="Status" onChange={handleChange} />
            <button type="submit">Add Job</button>
        </form>
    );
}

export default AddJob;