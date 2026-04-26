import { Search, X, Globe } from 'lucide-react';
import { useState } from 'react';

const JobFilters = ({ onFilterChange, onReset }) => {
  const [filters, setFilters] = useState({
    keyword: '',
    location: '',
    source: '',
    postedWithin: '',
    easyApplyOnly: false,
    minSalary: ''
  });

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    const newFilters = {
      ...filters,
      [name]: type === 'checkbox' ? checked : value
    };
    setFilters(newFilters);
    onFilterChange(newFilters);
  };

  const handleReset = () => {
    const cleared = {
      keyword: '',
      location: '',
      source: '',
      postedWithin: '',
      easyApplyOnly: false,
      minSalary: ''
    };
    setFilters(cleared);
    onReset();
  };

  const hasActiveFilters = Object.entries(filters).some(
    ([, v]) => v !== '' && v !== false
  );

  const isSingapore = filters.location.toLowerCase() === 'singapore';

  return (
    <div className="bg-white rounded-xl shadow-md p-6 mb-6">
      {/* Row 1 */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-6 gap-4">
        {/* Search */}
        <div className="lg:col-span-2">
          <label className="block text-sm font-medium text-gray-700 mb-2">Search</label>
          <div className="relative">
            <Search className="absolute left-3 top-3 text-gray-400" size={18} />
            <input
              type="text"
              name="keyword"
              placeholder="Job title or company…"
              value={filters.keyword}
              onChange={handleChange}
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
            />
          </div>
        </div>

        {/* Location */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Location</label>
          <select
            name="location"
            value={filters.location}
            onChange={handleChange}
            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
          >
            <option value="">All Locations</option>
            <option value="India">India</option>
            <option value="Singapore">Singapore</option>
            <option value="Remote">Remote</option>
            <option value="Chennai">Chennai</option>
            <option value="Bangalore">Bangalore</option>
            <option value="Hyderabad">Hyderabad</option>
            <option value="Pune">Pune</option>
            <option value="Mumbai">Mumbai</option>
            <option value="Noida">Noida</option>
          </select>
        </div>

        {/* Source */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Source</label>
          <select
            name="source"
            value={filters.source}
            onChange={handleChange}
            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
          >
            <option value="">All Sources</option>
            <option value="LinkedIn">LinkedIn</option>
            <option value="Naukri">Naukri</option>
            <option value="Indeed">Indeed</option>
            <option value="Glassdoor">Glassdoor</option>
          </select>
        </div>

        {/* Posted Within */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Posted Within</label>
          <select
            name="postedWithin"
            value={filters.postedWithin}
            onChange={handleChange}
            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
          >
            <option value="">Any Time</option>
            <option value="1">Last 24 hours</option>
            <option value="3">Last 3 days</option>
            <option value="7">Last 7 days</option>
            <option value="14">Last 14 days</option>
            <option value="30">Last 30 days</option>
          </select>
        </div>

        {/* Min Salary */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Min Salary (LPA)</label>
          <select
            name="minSalary"
            value={filters.minSalary}
            onChange={handleChange}
            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
          >
            <option value="">Any Salary</option>
            <option value="800000">8+ LPA</option>
            <option value="1000000">10+ LPA</option>
            <option value="1200000">12+ LPA</option>
            <option value="1500000">15+ LPA</option>
            <option value="2000000">20+ LPA</option>
          </select>
        </div>
      </div>

      {/* Singapore Live Jobs Banner */}
      {isSingapore && (
        <div className="mt-3 flex items-center gap-2 bg-blue-50 border border-blue-200 rounded-lg px-3 py-2 text-sm text-blue-700">
          <Globe size={15} className="text-blue-500 flex-shrink-0 animate-pulse" />
          <span><strong>Live mode active:</strong> Jobs will be fetched in real-time from LinkedIn, Indeed, Glassdoor, MyCareersFuture & more.</span>
        </div>
      )}

      {/* Row 2 */}
      <div className="flex items-center gap-4 mt-4">
        <label className="flex items-center gap-2 cursor-pointer">
          <input
            type="checkbox"
            name="easyApplyOnly"
            checked={filters.easyApplyOnly}
            onChange={handleChange}
            className="w-4 h-4 text-blue-600 rounded focus:ring-2 focus:ring-blue-600"
          />
          <span className="text-sm text-gray-700">Easy Apply Only</span>
        </label>

        {hasActiveFilters && (
          <button
            onClick={handleReset}
            className="ml-auto flex items-center gap-2 px-4 py-2 text-red-600 hover:text-red-800 font-medium text-sm"
          >
            <X size={16} />
            Clear All Filters
          </button>
        )}
      </div>
    </div>
  );
};

export default JobFilters;
