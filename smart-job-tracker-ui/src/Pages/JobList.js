import { useEffect, useState } from "react";

function JobList() {
  const [jobs, setJobs] = useState([]);

  useEffect(() => {
   fetch("http://localhost:5081/api/jobs")
      .then(res => res.json())
      .then(data => setJobs(data));
  }, []);

  return (
    <div>
      <h2>Job List</h2>
      {jobs.map(job => (
        <div key={job.id} style={{ border: "1px solid gray", margin: "10px", padding: "10px" }}>
          <h3>{job.company}</h3>
          <p>{job.role}</p>
          <p>Status: {job.status}</p>
        </div>
      ))}
    </div>
  );
}

export default JobList;