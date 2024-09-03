import { useState } from 'react';
import axios from 'axios';

export default function BlogGenerationNew() {
    const [seoPhrase, setSeoPhrase] = useState('');
    const [isGenerating, setIsGenerating] = useState(false);
    const [message, setMessage] = useState({ type: '', content: '' });

    const pollTaskStatus = async (taskId) => {
        try {
            const pollInterval = setInterval(async () => {
                const response = await axios.get(`/api/blog/status/${taskId}`);
                const { status } = response.data;

                if (status === 'Completed') {
                    clearInterval(pollInterval);
                    setMessage({ type: 'success', content: 'Blog created successfully!' });
                    setIsGenerating(false);
                } else if (status === 'Failed') {
                    clearInterval(pollInterval);
                    setMessage({ type: 'error', content: 'Failed to generate blog.' });
                    setIsGenerating(false);
                }
            }, 5000); // Poll every 5 seconds
        } catch (error) {
            console.error('Error while polling task status', error);
            setMessage({ type: 'error', content: 'Error while checking blog status.' });
            setIsGenerating(false);
        }
    };

    const handleGenerate = async () => {
        if (!seoPhrase.trim()) {
            setMessage({ type: 'error', content: 'Please enter an SEO phrase.' });
            return;
        }

        setIsGenerating(true);
        setMessage({ type: '', content: '' });

        try {
            const response = await axios.post('/api/blog/generate', { seoPhrase });

            const { taskId } = response.data;
            pollTaskStatus(taskId);
        } catch (error) {
            console.error('Error during blog generation', error);
            setMessage({ type: 'error', content: 'Failed to initiate blog generation.' });
            setIsGenerating(false);
        }
    };

    return (
        <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-center p-4">
            <div className="w-full max-w-md space-y-8">
                <div className="text-center">
                    <h1 className="text-3xl font-bold text-gray-900">Generate Blog</h1>
                    <p className="mt-2 text-sm text-gray-600">
                        Enter an SEO phrase and we'll generate a unique blog post for you.
                    </p>
                </div>

                <div className="mt-8 space-y-6">
                    <div>
                        <label htmlFor="seo-phrase" className="sr-only">
                            Enter SEO Phrase
                        </label>
                        <input
                            id="seo-phrase"
                            name="seo-phrase"
                            type="text"
                            required
                            className="appearance-none rounded-md relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-black focus:border-black focus:z-10 sm:text-sm"
                            placeholder="Enter SEO Phrase"
                            value={seoPhrase}
                            onChange={(e) => setSeoPhrase(e.target.value)}
                            disabled={isGenerating}
                        />
                    </div>

                    <div className="flex justify-center">
                        <button
                            onClick={handleGenerate}
                            disabled={isGenerating}
                            className="w-full max-w-sm flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-black hover:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-black"
                        >
                            {isGenerating ? (
                                <>
                                    <svg
                                        className="animate-spin -ml-1 mr-3 h-5 w-5 text-white"
                                        xmlns="http://www.w3.org/2000/svg"
                                        fill="none"
                                        viewBox="0 0 24 24"
                                    >
                                        <circle
                                            className="opacity-25"
                                            cx="12"
                                            cy="12"
                                            r="10"
                                            stroke="currentColor"
                                            strokeWidth="4"
                                        ></circle>
                                        <path
                                            className="opacity-75"
                                            fill="currentColor"
                                            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zM2 12a10 10 0 0010 10v-4a6 6 0 01-6-6H2z"
                                        ></path>
                                    </svg>
                                    Generating...
                                </>
                            ) : (
                                'Generate Blog'
                            )}
                        </button>
                    </div>

                    {message.content && (
                        <div
                            className={`mt-4 p-4 text-sm rounded-md text-center ${message.type === 'error'
                                ? 'bg-red-100 text-red-700'
                                : 'bg-green-100 text-green-700'
                                }`}
                        >
                            {message.content}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );

}
