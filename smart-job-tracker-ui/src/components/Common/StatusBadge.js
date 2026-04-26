const StatusBadge = ({ status }) => {
  const statusStyles = {
    Applied: "bg-blue-100 text-blue-800",
    Screening: "bg-yellow-100 text-yellow-800",
    Interviewing: "bg-purple-100 text-purple-800",
    Offered: "bg-green-100 text-green-800",
    Rejected: "bg-red-100 text-red-800",
    Withdrawn: "bg-gray-100 text-gray-800"
  };

  const style = statusStyles[status] || "bg-gray-100 text-gray-800";

  return (
    <span className={`inline-block px-3 py-1 rounded-full text-sm font-medium ${style}`}>
      {status}
    </span>
  );
};

export default StatusBadge;
