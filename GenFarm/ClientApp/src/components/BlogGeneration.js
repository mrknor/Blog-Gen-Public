import React, { Component } from 'react';
import axios from 'axios';

export class BlogGeneration extends Component {
    static displayName = BlogGeneration.name;

    constructor(props) {
        super(props);
        this.state = {
            seoPhrase: '',
            message: '',
            isGenerating: false,
        };
    }

    handleInputChange = (event) => {
        const newSeoPhrase = event.target.value;
        console.log(`SEO Phrase updated: ${newSeoPhrase}`); // Log input change
        this.setState({ seoPhrase: newSeoPhrase });
    };

    handleGenerateBlog = async () => {
        const { seoPhrase } = this.state;

        if (seoPhrase.trim() === '') {
            console.log('SEO phrase is empty'); // Log empty phrase
            this.setState({ message: 'SEO phrase is required.' });
            return;
        }

        console.log('Starting blog generation'); // Log start of generation
        this.setState({ isGenerating: true, message: '' });

        try {
            console.log(`Sending POST request to /api/blog/generate with SEO phrase: ${seoPhrase}`); // Log API call
            const response = await axios.post('/api/blog/generate', {
                seoPhrase: this.state.seoPhrase,
            });

            console.log('API call successful'); // Log successful API call
            console.log(`Response from backend: ${response.data}`); // Log response data

            this.setState({
                message: response.data, // Success message from backend
                isGenerating: false,
            });
        } catch (error) {
            console.error('Error during blog generation', error); // Log error details
            let errorMessage = 'Error: Could not generate blog.';
            if (error.response) {
                console.error(`Backend error response: ${error.response.data}`); // Log backend error
                errorMessage = `Error: ${error.response.data}`;
            }

            this.setState({
                message: errorMessage,
                isGenerating: false,
            });
        }
    };

    render() {
        const { seoPhrase, message, isGenerating } = this.state;

        console.log('Rendering BlogGeneration component'); // Log component rendering

        return (
            <div>
                <h2>Generate Blog</h2>
                <p>This component generates a blog based on an SEO phrase.</p>
                <input
                    type="text"
                    placeholder="Enter SEO Phrase"
                    value={seoPhrase}
                    onChange={this.handleInputChange}
                />
                <button onClick={this.handleGenerateBlog} disabled={isGenerating}>
                    {isGenerating ? 'Generating...' : 'Generate Blog'}
                </button>
                <div>{message}</div>
            </div>
        );
    }
}

export default BlogGeneration;
