import React, { useEffect, useState } from 'react';
import axios from 'axios';

function BlogList() {
    const [blogPosts, setBlogPosts] = useState([]);
    const [error, setError] = useState('');

    useEffect(() => {
        const fetchBlogPosts = async () => {
            try {
                const response = await axios.get('/api/blogpost/all');
                setBlogPosts(response.data);
            } catch (error) {
                setError('Error: Could not fetch blog posts.');
            }
        };

        fetchBlogPosts(); // Fetch blog posts on component mount
    }, []);

    return (
        <div>
            <h2>Blog List</h2>
            {error && <div>{error}</div>}
            {blogPosts.map((blog, index) => (
                <div key={index}>
                    <h3>{blog.Title}</h3>
                    <p>{blog.Content}</p>
                </div>
            ))}
        </div>
    );
}

export default BlogList;
