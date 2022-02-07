import React, { Component } from 'react'
import ReactPlayer from 'react-player'
import styled from 'styled-components'
import InfiniteScroll from "react-infinite-scroll-component"

const ContentDiv = styled.div`
    // display: grid;
    // grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
`;

export class Contents extends Component {
  static displayName = Contents.name;

  constructor(props) {
    super(props);
    this.state = { links: [], loading: true };
  }

  componentDidMount() {
      this.fetchMoreData()
  }

  static renderLinksTable(links) {
    return (
        links.map(link =>
              <ReactPlayer url={link.value} controls={true} loop={true} playing={true}/>
        )
    );
  }

  render() {
    // let contents = this.state.loading
    //   ? <p><em>Loading...</em></p>
    //   : Contents.renderLinksTable(this.state.links);

    return (
      <div>
        <p>This component demonstrates fetching data from the server.</p>
        <InfiniteScroll 
            next={this.fetchMoreData} 
            hasMore={true} 
            loader={<h4>Loading...</h4>} 
            dataLength={this.state.links.length}
        >
            <ContentDiv>
                {Contents.renderLinksTable(this.state.links)}
            </ContentDiv>
        </InfiniteScroll>
      </div>
    );
  }

  fetchMoreData = async () => {
      const url = 'links?' + new URLSearchParams({count: 10, looped: true})
      const response = await fetch(url)
      const data = await response.json();
      this.setState({ links: this.state.links.concat(data), loading: false });
  }
}
